using NotificationHub.Domain.Entities;

namespace NotificationHub.Application.Abstractions;

public interface INotificationProvider
{
    Task<bool> SendAsync(Notification notification, CancellationToken cancellationToken = default);
}