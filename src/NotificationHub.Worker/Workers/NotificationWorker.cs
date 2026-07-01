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

    // Retry backoff delays matching the architecture spec
    private static readonly TimeSpan[] RetryDelays =
    [
        TimeSpan.FromSeconds(1),
        TimeSpan.FromSeconds(5),
        TimeSpan.FromSeconds(15),
        TimeSpan.FromSeconds(30),
        TimeSpan.FromMinutes(1)
    ];

    private const int MaxRetries = 5;
    private const string DlqKey = "notification_dlq";

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
                await ProcessNextAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in worker loop");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private async Task ProcessNextAsync(CancellationToken stoppingToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var queue = scope.ServiceProvider.GetRequiredService<INotificationQueue>();
        var repository = scope.ServiceProvider.GetRequiredService<INotificationRepository>();
        var provider = scope.ServiceProvider.GetRequiredService<INotificationProvider>();

        var notificationId = await queue.DequeueAsync(stoppingToken);

        if (notificationId is null)
        {
            // Queue is empty — wait before polling again
            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            return;
        }

        var notification = await repository.GetByIdAsync(notificationId.Value, stoppingToken);

        if (notification is null)
        {
            _logger.LogWarning("Notification {Id} not found in DB — skipping", notificationId);
            return;
        }

        _logger.LogInformation("Processing notification {PublicId} (attempt {Attempt})",
            notification.PublicId, notification.RetryCount + 1);

        notification.Status = NotificationStatus.Processing;
        await repository.SaveChangesAsync(stoppingToken);

        try
        {
            var success = await provider.SendAsync(notification, stoppingToken);

            if (success)
            {
                notification.Status = NotificationStatus.Sent;
                _logger.LogInformation("Notification {PublicId} sent successfully", notification.PublicId);
            }
            else
            {
                await HandleFailureAsync(notification, queue, repository, stoppingToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Provider threw for notification {PublicId}", notification.PublicId);
            await HandleFailureAsync(notification, queue, repository, stoppingToken);
        }

        await repository.SaveChangesAsync(stoppingToken);
    }

    private async Task HandleFailureAsync(
        NotificationHub.Domain.Entities.Notification notification,
        INotificationQueue queue,
        INotificationRepository repository,
        CancellationToken stoppingToken)
    {
        notification.RetryCount++;

        if (notification.RetryCount >= MaxRetries)
        {
            notification.Status = NotificationStatus.DeadLetter;
            _logger.LogWarning(
                "Notification {PublicId} exceeded max retries — moving to DLQ", notification.PublicId);

            // Push to dead letter queue for manual ops replay
            await queue.EnqueueAsync(notification.Id, stoppingToken);
            return;
        }

        notification.Status = NotificationStatus.Retrying;
        var delay = RetryDelays[notification.RetryCount - 1];

        _logger.LogWarning(
            "Notification {PublicId} failed (attempt {Attempt}) — retrying in {Delay}",
            notification.PublicId, notification.RetryCount, delay);

        await Task.Delay(delay, stoppingToken);
        await queue.EnqueueAsync(notification.Id, stoppingToken);
    }
}