using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NotificationHub.Application.Abstractions;
using NotificationHub.Domain.Entities;
using StackExchange.Redis;

namespace NotificationHub.Worker.Workers;

public class ImportWorker : BackgroundService
{
    private const int BatchSize = 1000;
    private static readonly TimeSpan BetweenBatchesDelay = TimeSpan.FromMilliseconds(200);

    private readonly string _workerId = $"import-{Guid.NewGuid():N}";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<ImportWorker> _logger;

    public ImportWorker(
        IServiceScopeFactory scopeFactory,
        IConnectionMultiplexer redis,
        ILogger<ImportWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _redis = redis;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ImportWorker {WorkerId} started", _workerId);
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await WriteHeartbeatAsync(stoppingToken);

            try
            {
                await StartPendingImportJobsAsync(stoppingToken);
                await ProcessRunningImportJobsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ImportWorker loop");
            }

            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }

        await RemoveHeartbeatAsync();
        _logger.LogInformation("ImportWorker {WorkerId} stopped", _workerId);
    }

    // ─── Heartbeat ───────────────────────────────────────────────────────────

    private async Task WriteHeartbeatAsync(CancellationToken stoppingToken)
    {
        try
        {
            await _redis.GetDatabase().StringSetAsync(
                key: $"worker:heartbeat:{_workerId}",
                value: DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                expiry: TimeSpan.FromSeconds(90));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to write heartbeat for ImportWorker {WorkerId}", _workerId);
        }
    }

    private async Task RemoveHeartbeatAsync()
    {
        try
        {
            await _redis.GetDatabase().KeyDeleteAsync($"worker:heartbeat:{_workerId}");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to remove heartbeat for ImportWorker {WorkerId}", _workerId);
        }
    }

    // ─── Pending → Running ───────────────────────────────────────────────────

    private async Task StartPendingImportJobsAsync(CancellationToken stoppingToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var importJobRepository = scope.ServiceProvider.GetRequiredService<IImportJobRepository>();

        var pending = await importJobRepository.GetPendingAsync(stoppingToken);
        if (pending.Count == 0) return;

        foreach (var job in pending)
        {
            job.Start();
            _logger.LogInformation("ImportJob {Id} starting — table {Table}", job.Id, job.TableName);
        }

        await importJobRepository.SaveChangesAsync(stoppingToken);
    }

    // ─── Running jobs: read batches to completion, resumable on interrupt ────

    private async Task ProcessRunningImportJobsAsync(CancellationToken stoppingToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var importJobRepository = scope.ServiceProvider.GetRequiredService<IImportJobRepository>();
        var dataSourceRepository = scope.ServiceProvider.GetRequiredService<IDataSourceRepository>();
        var encryptionService = scope.ServiceProvider.GetRequiredService<IEncryptionService>();
        var importDataReader = scope.ServiceProvider.GetRequiredService<IImportDataReader>();
        var campaignService = scope.ServiceProvider.GetRequiredService<ICampaignService>();

        var running = await importJobRepository.GetRunningAsync(stoppingToken);

        foreach (var job in running)
        {
            if (stoppingToken.IsCancellationRequested) break;

            try
            {
                await RunImportJobAsync(
                    job, dataSourceRepository, encryptionService, importDataReader,
                    campaignService, importJobRepository, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ImportJob {Id} failed", job.Id);
                job.Fail(ex.Message);
                await importJobRepository.SaveChangesAsync(stoppingToken);
            }
        }
    }

    private async Task RunImportJobAsync(
        ImportJob job,
        IDataSourceRepository dataSourceRepository,
        IEncryptionService encryptionService,
        IImportDataReader importDataReader,
        ICampaignService campaignService,
        IImportJobRepository importJobRepository,
        CancellationToken stoppingToken)
    {
        var dataSource = await dataSourceRepository.GetByIdAsync(job.DataSourceId, job.OrganizationId, stoppingToken);
        if (dataSource is null)
        {
            job.Fail("Data source no longer exists.");
            await importJobRepository.SaveChangesAsync(stoppingToken);
            return;
        }

        var connectionString = encryptionService.Decrypt(dataSource.EncryptedConnectionString);

        // Loop batches to completion. On cancellation, break mid-import —
        // LastCursorId is already persisted from the prior batch, so the
        // next worker pickup resumes exactly where this one stopped.
        while (!stoppingToken.IsCancellationRequested)
        {
            var batch = await importDataReader.ReadBatchAsync(
                dataSource.Type, connectionString, job.TableName, job.PrimaryKeyColumn,
                job.EmailColumn, job.FirstNameColumn, job.LastNameColumn, job.WhereClause,
                job.LastCursorId, BatchSize, stoppingToken);

            if (batch.Rows.Count == 0)
            {
                job.Complete();
                _logger.LogInformation(
                    "ImportJob {Id} completed — {Rows} rows read, {Added} recipients added",
                    job.Id, job.RowsRead, job.RecipientsAdded);
                await importJobRepository.SaveChangesAsync(stoppingToken);
                break;
            }

            var validRows = batch.Rows
                .Where(r => !string.IsNullOrWhiteSpace(r.Email) && IsValidEmail(r.Email))
                .ToList();

            var skippedInvalid = batch.Rows.Count - validRows.Count;
            if (skippedInvalid > 0)
                job.RecordError($"{skippedInvalid} row(s) skipped in batch: missing or invalid email.");

            var recipients = validRows
                .Select(r => new ImportedRecipient(r.Email, r.FirstName, r.LastName))
                .ToList();

            var result = await campaignService.AddRecipientsWithNamesAsync(
                new(job.CampaignId, job.OrganizationId, recipients), stoppingToken);

            job.RecordBatch(batch.Rows.Count, result.Added, batch.LastCursor);
            await importJobRepository.SaveChangesAsync(stoppingToken);
            await WriteHeartbeatAsync(stoppingToken);

            _logger.LogInformation(
                "ImportJob {Id} — batch read: {Rows} rows, {Added} added, {Skipped} skipped (cursor {Cursor})",
                job.Id, batch.Rows.Count, result.Added, result.Skipped, batch.LastCursor);

            await Task.Delay(BetweenBatchesDelay, stoppingToken);
        }
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}