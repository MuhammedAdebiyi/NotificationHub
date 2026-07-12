using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NotificationHub.Application.Abstractions;
using NotificationHub.Domain.Enums;
using StackExchange.Redis;

namespace NotificationHub.Worker.Workers;

public class NotificationWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<NotificationWorker> _logger;

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
        _logger.LogInformation("NotificationWorker started");

        while (!stoppingToken.IsCancellationRequested)
        {
            await WriteHeartbeatAsync(stoppingToken);

            try
            {
                await ProcessNextAsync(stoppingToken);
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

        _logger.LogInformation("NotificationWorker stopped");
    }

    private async Task WriteHeartbeatAsync(CancellationToken stoppingToken)
    {
        try
        {
            var db = _redis.GetDatabase();
            await db.StringSetAsync(
                "worker:online:count",
                "1",
                TimeSpan.FromSeconds(60));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to write worker heartbeat to Redis");
        }
    }

    private async Task ProcessNextAsync(CancellationToken stoppingToken)
    {
        var db = _redis.GetDatabase();
        var value = await db.ListRightPopAsync("notification_queue");

        if (value.IsNullOrEmpty)
        {
            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            return;
        }

        if (!Guid.TryParse((string?)value, out var notificationId))
        {
            _logger.LogWarning("Invalid notification ID in queue: {Value}", (string?)value);
            return;
        }

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

        notification.Status = NotificationStatus.Processing;
        await repository.SaveChangesAsync(stoppingToken);

        var success = await provider.SendAsync(notification, stoppingToken);

        if (success)
        {
            notification.Status = NotificationStatus.Sent;
            await repository.SaveChangesAsync(stoppingToken);
            _logger.LogInformation("Notification {Id} sent successfully", notification.Id);
        }
        else
        {
            await HandleFailureAsync(notification, repository, db, stoppingToken);
        }
    }

    private async Task HandleFailureAsync(
        NotificationHub.Domain.Entities.Notification notification,
        INotificationRepository repository,
        IDatabase db,
        CancellationToken stoppingToken)
    {
        notification.RetryCount++;

        var delays = new[] { 1, 5, 15, 30, 60 };

        if (notification.RetryCount <= delays.Length)
        {
            notification.Status = NotificationStatus.Retrying;
            await repository.SaveChangesAsync(stoppingToken);

            var delay = delays[notification.RetryCount - 1];
            _logger.LogWarning("Notification {Id} failed, retrying in {Delay}s (attempt {Attempt})",
                notification.Id, delay, notification.RetryCount);

            await Task.Delay(TimeSpan.FromSeconds(delay), stoppingToken);
            await db.ListLeftPushAsync("notification_queue", notification.Id.ToString());
        }
        else
        {
            notification.Status = NotificationStatus.DeadLetter;
            await repository.SaveChangesAsync(stoppingToken);

            await db.ListLeftPushAsync("notification_dlq", notification.Id.ToString());
            _logger.LogError("Notification {Id} moved to DLQ after {Attempts} attempts",
                notification.Id, notification.RetryCount);
        }
    }
}