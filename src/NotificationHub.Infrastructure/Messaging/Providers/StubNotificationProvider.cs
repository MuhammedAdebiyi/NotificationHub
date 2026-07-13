using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using NotificationHub.Application.Abstractions;
using NotificationHub.Domain.Entities;
using NotificationHub.Domain.Enums;
using NotificationHub.Infrastructure.Persistence;

namespace NotificationHub.Infrastructure.Messaging.Providers;

public class StubNotificationProvider : INotificationProvider
{
    private readonly IEmailProvider _emailProvider;
    private readonly ILogger<StubNotificationProvider> _logger;
    private readonly AppDbContext _context;

    public StubNotificationProvider(
        IEmailProvider emailProvider,
        ILogger<StubNotificationProvider> logger,
        AppDbContext context)
    {
        _emailProvider = emailProvider;
        _logger = logger;
        _context = context;
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
                        "Channel {Channel} not yet implemented — notification {Id} skipped",
                        notification.Channel, notification.Id);

                    await WriteLogAsync(notification, "Stub", "Channel not implemented — skipped", cancellationToken);
                    return true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Provider failed for notification {Id}", notification.Id);
            await WriteLogAsync(notification, "SendByte", $"error: {ex.Message}", cancellationToken);
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

        var to = notification.RecipientEmail;

        var message = new EmailMessage(
            From: "NotificationHub <noreply@coursevaultai.app>",
            To: to,
            Subject: payload.Subject,
            Html: $"<p>{payload.Body}</p>",
            Text: payload.Body
        );

        var emailId = await _emailProvider.SendAsync(message, cancellationToken);

        _logger.LogInformation(
            "Email sent via SendByte for notification {Id} to {To}",
            notification.Id, to);

        await WriteLogAsync(
            notification,
            "SendByte",
            $"accepted: id={emailId}",
            cancellationToken);

        return true;
    }

    private async Task WriteLogAsync(
        Notification notification,
        string provider,
        string response,
        CancellationToken cancellationToken)
    {
        try
        {
            var log = new NotificationLog
            {
                NotificationId = notification.Id,
                OrganizationId = notification.OrganizationId,
                Provider = provider,
                Response = response,
            };
            _context.NotificationLogs.Add(log);
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to write notification log for {Id}", notification.Id);
        }
    }

    private record EmailPayload(
        [property: JsonPropertyName("subject")] string Subject,
        [property: JsonPropertyName("body")] string Body
    );
}