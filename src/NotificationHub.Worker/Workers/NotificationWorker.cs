using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NotificationHub.Application.Abstractions;
using NotificationHub.Domain.Entities;
using NotificationHub.Domain.Enums;
using NotificationHub.Domain.Exceptions;
using StackExchange.Redis;

namespace NotificationHub.Worker.Workers;

public class NotificationWorker : BackgroundService
{
    // Each worker instance gets a unique ID so Analytics can count
    // individual workers via the "worker:heartbeat:*" key pattern.
    private readonly string _workerId = $"notification-{Guid.NewGuid():N}";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<NotificationWorker> _logger;

    // ─── Concurrency ─────────────────────────────────────────────────────────
    private const int MaxConcurrency = 50;
    private readonly SemaphoreSlim _semaphore = new(MaxConcurrency, MaxConcurrency);

    // ─── Redis keys ──────────────────────────────────────────────────────────
    private const string QueueKey = "notification_queue";
    private const string RetryScheduleKey = "notification_retry_schedule";
    private const string DlqKey = "notification_dlq";

    private static readonly TimeSpan[] RetryDelays =
    {
        TimeSpan.FromSeconds(1),
        TimeSpan.FromSeconds(5),
        TimeSpan.FromSeconds(15),
        TimeSpan.FromSeconds(30),
        TimeSpan.FromSeconds(60),
    };

    // Atomically moves every item whose retry score (unix seconds) is <= now
    // from the sorted set into the working queue. Runs as a single Redis
    // operation so multiple worker instances can't both promote the same item.
    private const string PromoteDueScript = @"
        local due = redis.call('ZRANGEBYSCORE', KEYS[1], '-inf', ARGV[1])
        if #due > 0 then
            for i, id in ipairs(due) do
                redis.call('ZREM', KEYS[1], id)
                redis.call('LPUSH', KEYS[2], id)
            end
        end
        return #due
    ";

    public NotificationWorker(
        IServiceScopeFactory scopeFactory,
        IConnectionMultiplexer redis,
        ILogger<NotificationWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _redis = redis;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "NotificationWorker {WorkerId} started (max concurrency: {MaxConcurrency})",
            _workerId, MaxConcurrency);

        while (!stoppingToken.IsCancellationRequested)
        {
            // Heartbeat first — Analytics reads these keys to count online workers.
            await WriteHeartbeatAsync(stoppingToken);

            try
            {
                await PromoteDueRetriesAsync(stoppingToken);

                var processed = await ProcessBatchAsync(stoppingToken);

                if (processed == 0)
                {
                    // Nothing to do — avoid a tight busy loop hammering Redis.
                    await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error in NotificationWorker loop");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        // Remove heartbeat immediately on clean shutdown so Analytics
        // doesn't report a ghost worker for the remaining TTL window.
        await RemoveHeartbeatAsync();

        _logger.LogInformation("NotificationWorker {WorkerId} stopped", _workerId);
    }

    // ─── Heartbeat ───────────────────────────────────────────────────────────

    private async Task WriteHeartbeatAsync(CancellationToken stoppingToken)
    {
        try
        {
            var db = _redis.GetDatabase();

            // TTL = 10 seconds. If the worker crashes the key expires automatically
            // and Analytics correctly reports it as offline.
            await db.StringSetAsync(
                key: $"worker:heartbeat:{_workerId}",
                value: DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                expiry: TimeSpan.FromSeconds(10));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to write heartbeat for worker {WorkerId}", _workerId);
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
            _logger.LogWarning(ex, "Failed to remove heartbeat for worker {WorkerId}", _workerId);
        }
    }

    // ─── Retry scheduling ────────────────────────────────────────────────────

    // Moves any notification whose retry due-time has passed from the sorted
    // set back into the working queue. Cheap no-op when nothing is due.
    private async Task PromoteDueRetriesAsync(CancellationToken stoppingToken)
    {
        var db = _redis.GetDatabase();
        var nowUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        var result = await db.ScriptEvaluateAsync(
            PromoteDueScript,
            keys: new RedisKey[] { RetryScheduleKey, QueueKey },
            values: new RedisValue[] { nowUnix });

        var promoted = (int)result;

        if (promoted > 0)
        {
            _logger.LogDebug("Promoted {Count} due retries into the queue", promoted);
        }
    }

    // ─── Batch processing ────────────────────────────────────────────────────

    // Pops up to MaxConcurrency items in one round trip and fans them out
    // concurrently, gated by the semaphore. Returns how many were dequeued.
    private async Task<int> ProcessBatchAsync(CancellationToken stoppingToken)
    {
        var db = _redis.GetDatabase();

        var values = await db.ListRightPopAsync(QueueKey, MaxConcurrency);

        if (values is null || values.Length == 0)
        {
            return 0;
        }

        var tasks = new List<Task>(values.Length);

        foreach (var value in values)
        {
            if (!Guid.TryParse((string?)value, out var notificationId))
            {
                _logger.LogWarning("Invalid notification ID in queue: {Value}", (string?)value);
                continue;
            }

            await _semaphore.WaitAsync(stoppingToken);

            tasks.Add(ProcessOneGuardedAsync(notificationId, stoppingToken));
        }

        await Task.WhenAll(tasks);

        return values.Length;
    }

    // Wraps ProcessOneAsync with the semaphore release + top-level exception
    // handling so one bad notification can't take down the whole batch.
    private async Task ProcessOneGuardedAsync(Guid notificationId, CancellationToken stoppingToken)
    {
        try
        {
            await ProcessOneAsync(notificationId, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // Shutdown mid-send — not a failure, just stop quietly.
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error processing notification {Id}", notificationId);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task ProcessOneAsync(Guid notificationId, CancellationToken stoppingToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<INotificationRepository>();
        var provider = scope.ServiceProvider.GetRequiredService<INotificationProvider>();

        var notification = await repository.GetByIdAsync(notificationId, stoppingToken);
        if (notification is null)
        {
            _logger.LogWarning("Notification {Id} not found in DB", notificationId);
            return;
        }

        _logger.LogInformation("Processing notification {Id} (attempt {Attempt})",
            notification.Id, notification.RetryCount + 1);

        notification.AssignWorker(_workerId);
        await repository.SaveChangesAsync(stoppingToken);

        bool success;
        try
        {
            success = await provider.SendAsync(notification, stoppingToken);
        }
        catch (NonRetriableNotificationException ex)
        {
            // Bad payload / invalid recipient — no amount of retrying fixes this.
            // Skip the normal backoff entirely and go straight to DLQ.
            await MoveToDeadLetterAsync(notification, ex.Message, repository, stoppingToken);
            return;
        }

        if (success)
        {
            notification.MarkDelivered(
                provider.GetType().Name,
                string.Empty
            );

            await repository.SaveChangesAsync(stoppingToken);

            _logger.LogInformation(
                "Notification {Id} sent successfully",
                notification.Id);
        }
        else
        {
            await HandleFailureAsync(notification, repository, stoppingToken);
        }
    }

    // Schedules a retry by writing a due-time into the sorted set instead of
    // blocking this task with Task.Delay. The worker stays free to process
    // other queue items while this notification waits; PromoteDueRetriesAsync
    // picks it back up once its score (unix timestamp) has passed.
    private async Task HandleFailureAsync(
        Notification notification,
        INotificationRepository repository,
        CancellationToken stoppingToken)
    {
        var nextAttempt = notification.RetryCount + 1;

        if (nextAttempt <= RetryDelays.Length)
        {
            var delay = RetryDelays[nextAttempt - 1];
            var dueAt = DateTime.UtcNow.Add(delay);

            notification.ScheduleRetry(dueAt, "Provider delivery failed");
            await repository.SaveChangesAsync(stoppingToken);

            var db = _redis.GetDatabase();
            var dueUnix = new DateTimeOffset(dueAt, TimeSpan.Zero).ToUnixTimeSeconds();

            await db.SortedSetAddAsync(
                RetryScheduleKey,
                notification.Id.ToString(),
                dueUnix);

            _logger.LogWarning(
                "Notification {Id} failed, scheduled retry at {DueAt:o} in {Delay}s (attempt {Attempt})",
                notification.Id,
                dueAt,
                delay.TotalSeconds,
                nextAttempt);
        }
        else
        {
            await MoveToDeadLetterAsync(notification, "Provider delivery failed", repository, stoppingToken);
        }
    }

    private async Task MoveToDeadLetterAsync(
        Notification notification,
        string reason,
        INotificationRepository repository,
        CancellationToken stoppingToken)
    {
        notification.MoveToDeadLetter(reason);

        await repository.SaveChangesAsync(stoppingToken);

        var db = _redis.GetDatabase();
        await db.ListLeftPushAsync(DlqKey, notification.Id.ToString());

        _logger.LogError(
            "Notification {Id} moved to DLQ — {Reason} (after {Attempts} attempt(s))",
            notification.Id,
            reason,
            notification.RetryCount);
    }

    public override void Dispose()
    {
        _semaphore.Dispose();
        base.Dispose();
    }
}