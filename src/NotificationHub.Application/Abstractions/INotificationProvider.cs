using NotificationHub.Domain.Entities;

namespace NotificationHub.Application.Abstractions;

public interface INotificationProvider
{
    string? LastProviderType { get; }

    /// <summary>Provider's message id from the most recent successful send
    /// (e.g. SendByte em_...). Used to match delivery webhooks.</summary>
    string? LastMessageId { get; }

    Task<bool> SendAsync(Notification notification, CancellationToken cancellationToken = default);
}