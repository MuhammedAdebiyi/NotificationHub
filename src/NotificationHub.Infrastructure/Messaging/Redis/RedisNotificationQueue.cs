using Microsoft.Extensions.Logging;
using NotificationHub.Application.Abstractions;
using StackExchange.Redis;

namespace NotificationHub.Infrastructure.Messaging.Redis;

public class RedisNotificationQueue : INotificationQueue
{
    private readonly IDatabase _db;
    private readonly ILogger<RedisNotificationQueue> _logger;
    private const string QueueKey = "notification_queue";
    private const string DlqKey = "notification_dlq";

    public RedisNotificationQueue(IConnectionMultiplexer redis, ILogger<RedisNotificationQueue> logger)
    {
        _db = redis.GetDatabase();
        _logger = logger;
    }

    public async Task EnqueueAsync(Guid notificationId, CancellationToken cancellationToken = default)
    {
        try
        {
            await _db.ListLeftPushAsync(QueueKey, notificationId.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis unavailable for enqueue — notification {Id} saved to DB, worker will catch it on next cycle", notificationId);
        }
    }

    public async Task EnqueueDeadLetterAsync(Guid notificationId, CancellationToken cancellationToken = default)
    {
        try
        {
            await _db.ListLeftPushAsync(DlqKey, notificationId.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis unavailable for DLQ enqueue — notification {Id} marked DLQ in DB", notificationId);
        }
    }

    public async Task<Guid?> DequeueAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var value = await _db.ListRightPopAsync(QueueKey);
            if (value.IsNullOrEmpty) return null;
            return Guid.Parse((string)value!);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis unavailable for dequeue");
            return null;
        }
    }
}