using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NotificationHub.Application.Abstractions;
using NotificationHub.Domain.Enums;

namespace NotificationHub.Worker.Workers;

public class NotificationWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<NotificationWorker> _logger;
    private readonly SemaphoreSlim _semaphore = new(50);

    private static readonly TimeSpan[] RetryDelays =
    [
        TimeSpan.FromSeconds(1),
        TimeSpan.FromSeconds(5),
        TimeSpan.FromSeconds(15),
        TimeSpan.FromSeconds(30),
        TimeSpan.FromMinutes(1)
    ];

    private const int MaxRetries = 5;

    public NotificationWorker(IServiceScopeFactory scopeFactory, ILogger<NotificationWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("NotificationWorker started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var notificationId = await PeekQueueAsync(stoppingToken);

                if (notificationId is null)
                {
                    await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
                    continue;
                }

                _logger.LogInformation(">>> Dequeued {Id} — acquiring semaphore", notificationId);
                await _semaphore.WaitAsync(stoppingToken);
                _logger.LogInformation(">>> Semaphore acquired — firing task for {Id}", notificationId);

                var idToProcess = notificationId.Value;

                _ = Task.Run(async () =>
                {
                    try
                    {
                        _logger.LogInformation(">>> Task.Run started for {Id}", idToProcess);
                        await ProcessNotificationAsync(idToProcess, CancellationToken.None);
                        _logger.LogInformation(">>> Task.Run completed for {Id}", idToProcess);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, ">>> Task.Run EXCEPTION for {Id}", idToProcess);
                    }
                    finally
                    {
                        _semaphore.Release();
                    }
                });
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in worker loop");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private async Task<Guid?> PeekQueueAsync(CancellationToken stoppingToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var queue = scope.ServiceProvider.GetRequiredService<INotificationQueue>();
        return await queue.DequeueAsync(stoppingToken);
    }

    private async Task ProcessNotificationAsync(Guid notificationId, CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<INotificationRepository>();
        var provider = scope.ServiceProvider.GetRequiredService<INotificationProvider>();
        var queue = scope.ServiceProvider.GetRequiredService<INotificationQueue>();

        var notification = await repository.GetByIdAsync(notificationId, cancellationToken);

        if (notification is null)
        {
            _logger.LogWarning("Notification {Id} not found in DB — skipping", notificationId);
            return;
        }

        _logger.LogInformation("Processing notification {PublicId} (attempt {Attempt})",
            notification.PublicId, notification.RetryCount + 1);

        notification.Status = NotificationStatus.Processing;
        await repository.SaveChangesAsync(cancellationToken);

        try
        {
            var success = await provider.SendAsync(notification, cancellationToken);

            if (success)
            {
                notification.Status = NotificationStatus.Sent;
                _logger.LogInformation("Notification {PublicId} sent successfully", notification.PublicId);
            }
            else
            {
                await HandleFailureAsync(notification, queue, repository, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Provider threw for notification {PublicId}", notification.PublicId);
            await HandleFailureAsync(notification, queue, repository, cancellationToken);
        }

        await repository.SaveChangesAsync(cancellationToken);
    }

    private async Task HandleFailureAsync(
        NotificationHub.Domain.Entities.Notification notification,
        INotificationQueue queue,
        INotificationRepository repository,
        CancellationToken cancellationToken)
    {
        notification.RetryCount++;

        if (notification.RetryCount >= MaxRetries)
        {
            notification.Status = NotificationStatus.DeadLetter;
            _logger.LogWarning("Notification {PublicId} exceeded max retries — moving to DLQ",
                notification.PublicId);
            await queue.EnqueueDeadLetterAsync(notification.Id, cancellationToken);
            return;
        }

        notification.Status = NotificationStatus.Retrying;
        var delay = RetryDelays[notification.RetryCount - 1];

        _logger.LogWarning("Notification {PublicId} failed (attempt {Attempt}) — retrying in {Delay}",
            notification.PublicId, notification.RetryCount, delay);

        await Task.Delay(delay, cancellationToken);
        await queue.EnqueueAsync(notification.Id, cancellationToken);
    }
}