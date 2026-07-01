using Microsoft.Extensions.Logging;
using NotificationHub.Application.Abstractions;
using NotificationHub.Domain.Entities;

namespace NotificationHub.Infrastructure.Messaging.Providers;

public class StubNotificationProvider : INotificationProvider
{
    private readonly ILogger<StubNotificationProvider> _logger;

    public StubNotificationProvider(ILogger<StubNotificationProvider> logger)
    {
        _logger = logger;
    }

    public Task<bool> SendAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "STUB: Would send {Channel} notification {PublicId} of type {Type} to user {UserId}",
            notification.Channel,
            notification.PublicId,
            notification.Type,
            notification.UserId);

        return Task.FromResult(true);
    }
}