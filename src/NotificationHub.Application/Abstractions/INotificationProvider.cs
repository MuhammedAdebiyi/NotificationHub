using NotificationHub.Domain.Entities;

namespace NotificationHub.Application.Abstractions;

public interface INotificationProvider
{
    string? LastProviderType { get; }
    Task<bool> SendAsync(Notification notification, CancellationToken cancellationToken = default);
}