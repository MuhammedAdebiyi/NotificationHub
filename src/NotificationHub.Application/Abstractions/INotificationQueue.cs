namespace NotificationHub.Application.Abstractions;

public interface INotificationQueue
{
    Task EnqueueAsync(Guid notificationId, CancellationToken cancellationToken = default);
    Task<Guid?> DequeueAsync(CancellationToken cancellationToken = default);
}