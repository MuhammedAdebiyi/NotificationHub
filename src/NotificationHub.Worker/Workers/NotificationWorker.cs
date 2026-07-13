using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NotificationHub.Application.Abstractions;
using NotificationHub.Domain.Entities;
using NotificationHub.Domain.Enums;
using StackExchange.Redis;

namespace NotificationHub.Worker.Workers;

public class NotificationWorker : BackgroundService
{
    // Each worker instance gets a unique ID so Analytics can count
    // individual workers via the "worker:heartbeat:*" key pattern.
    private readonly string _workerId = $"notification-{Guid.NewGuid():N}";

    private readonly IServiceScopeFactory      _scopeFactory;
    private readonly IConnectionMultiplexer    _redis;
    private readonly ILogger<NotificationWorker> _logger;

    public NotificationWorker(
        IServiceScopeFactory scopeFactory,
        IConnectionMultiplexer redis,
        ILogger<NotificationWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _redis        = redis;
        _logger       = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("NotificationWorker {WorkerId} started", _workerId);

        while (!stoppingToken.IsCancellationRequested)
        {
            // Heartbeat first — Analytics reads these keys to count online workers.
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
                key:   $"worker:heartbeat:{_workerId}",
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

    // ─── Processing ──────────────────────────────────────────────────────────

    private async Task ProcessNextAsync(CancellationToken stoppingToken)
    {
        var db    = _redis.GetDatabase();
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

        await using var scope      = _scopeFactory.CreateAsyncScope();
        var repository             = scope.ServiceProvider.GetRequiredService<INotificationRepository>();
        var provider               = scope.ServiceProvider.GetRequiredService<INotificationProvider>();

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

        var success = await provider.SendAsync(notification, stoppingToken);

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
            await HandleFailureAsync(notification, repository, db, stoppingToken);
        }
    }

        private async Task HandleFailureAsync(
        Notification notification,
        INotificationRepository repository,
        IDatabase db,
        CancellationToken stoppingToken)
    {
        var delays = new[] { 1, 5, 15, 30, 60 };

        var nextAttempt = notification.RetryCount + 1;

        if (nextAttempt <= delays.Length)
        {
            var delay = delays[nextAttempt - 1];

            notification.ScheduleRetry(
                DateTime.UtcNow.AddSeconds(delay),
                "Provider delivery failed");

            await repository.SaveChangesAsync(stoppingToken);

            _logger.LogWarning(
                "Notification {Id} failed, retrying in {Delay}s (attempt {Attempt})",
                notification.Id,
                delay,
                nextAttempt);

            await Task.Delay(TimeSpan.FromSeconds(delay), stoppingToken);

            await db.ListLeftPushAsync(
                "notification_queue",
                notification.Id.ToString());
        }
        else
        {
            notification.MoveToDeadLetter(
                "Provider delivery failed");

            await repository.SaveChangesAsync(stoppingToken);

            await db.ListLeftPushAsync(
                "notification_dlq",
                notification.Id.ToString());

            _logger.LogError(
                "Notification {Id} moved to DLQ after {Attempts} attempts",
                notification.Id,
                notification.RetryCount);
        }
    }
    }