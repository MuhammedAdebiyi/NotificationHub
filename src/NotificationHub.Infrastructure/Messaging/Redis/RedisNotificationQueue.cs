using NotificationHub.Application.Abstractions;
using StackExchange.Redis;

namespace NotificationHub.Infrastructure.Messaging.Redis;

public class RedisNotificationQueue : INotificationQueue
{
    private readonly IDatabase _db;
    private const string QueueKey = "notification_queue";
    private const string DlqKey = "notification_dlq";

    public RedisNotificationQueue(IConnectionMultiplexer redis)
    {
        _db = redis.GetDatabase();
    }

    public async Task EnqueueAsync(Guid notificationId, CancellationToken cancellationToken = default)
        => await _db.ListLeftPushAsync(QueueKey, notificationId.ToString());

    public async Task EnqueueDeadLetterAsync(Guid notificationId, CancellationToken cancellationToken = default)
        => await _db.ListLeftPushAsync(DlqKey, notificationId.ToString());

    public async Task<Guid?> DequeueAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var value = await _db.ListRightPopAsync(QueueKey);
            if (value.IsNullOrEmpty) return null;
            return Guid.Parse((string)value!);
        }
        catch (RedisTimeoutException)
        {
            return null; 
        }
    }
}