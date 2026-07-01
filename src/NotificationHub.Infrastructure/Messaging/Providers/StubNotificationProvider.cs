using System.Text.Json;
using Microsoft.Extensions.Logging;
using NotificationHub.Application.Abstractions;
using NotificationHub.Domain.Entities;
using NotificationHub.Domain.Enums;
using System.Text.Json.Serialization;

namespace NotificationHub.Infrastructure.Messaging.Providers;

public class StubNotificationProvider : INotificationProvider
{
    private readonly IEmailProvider _emailProvider;
    private readonly ILogger<StubNotificationProvider> _logger;

    public StubNotificationProvider(
        IEmailProvider emailProvider,
        ILogger<StubNotificationProvider> logger)
    {
        _emailProvider = emailProvider;
        _logger = logger;
    }

    public async Task<bool> SendAsync(
        Notification notification,
        CancellationToken cancellationToken = default)
    {
        try
        {
            switch (notification.Channel)
            {
                case NotificationChannel.Email:
                    return await SendEmailAsync(notification, cancellationToken);

                default:
                    _logger.LogInformation(
                        "STUB: {Channel} not yet implemented — notification {Id} skipped",
                        notification.Channel, notification.Id);
                    return true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Provider failed for notification {Id}", notification.Id);
            return false;
        }
    }

    private async Task<bool> SendEmailAsync(
        Notification notification,
        CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Deserialize<EmailPayload>(notification.Payload)
            ?? throw new InvalidOperationException(
                $"Could not deserialize email payload for notification {notification.Id}");

        var message = new EmailMessage(
            From: "NotificationHub <no-reply@coursevaultai.app>",
            To: payload.To,
            Subject: payload.Subject,
            Html: $"<p>{payload.Body}</p>",
            Text: payload.Body
            
        );

        await _emailProvider.SendAsync(message, cancellationToken);

        _logger.LogInformation(
            "Email sent via SendByte for notification {Id} to {To}",
            notification.Id, payload.To);

        return true;
    }

    private record EmailPayload(
    [property: JsonPropertyName("to")] string To,
    [property: JsonPropertyName("subject")] string Subject,
    [property: JsonPropertyName("body")] string Body
);
}