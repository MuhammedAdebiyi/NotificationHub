using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NotificationHub.Application.Abstractions;
using NotificationHub.Domain.Entities;
using NotificationHub.Domain.Enums;
using NotificationHub.Infrastructure.Persistence;
using NotificationHub.Domain.Exceptions;

namespace NotificationHub.Infrastructure.Messaging.Providers;

public class StubNotificationProvider : INotificationProvider
{
    private const string CampaignFromEmail = "campaigns@coursevaultai.app";

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

                    await WriteLogAsync(notification, "Stub", "Channel not implemented — skipped", isSuccess: true, cancellationToken);
                    return true;
            }
        }
        catch (OperationCanceledException)
        {
        
            throw;
        }
        catch (NonRetriableNotificationException)
        {
        
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Provider failed for notification {Id}", notification.Id);
            await WriteLogAsync(notification, "Resend", $"error: {ex.Message}", isSuccess: false, cancellationToken);
            return false;
        }
    }
     
    private async Task<bool> SendEmailAsync(
        Notification notification,
        CancellationToken cancellationToken)
    {
        EmailPayload? payload;
        try
        {
            payload = JsonSerializer.Deserialize<EmailPayload>(notification.Payload);
        }
        catch (JsonException ex)
        {
            throw new NonRetriableNotificationException(
                $"Malformed email payload for notification {notification.Id}", ex);
        }

        if (payload is null)
        {
            throw new NonRetriableNotificationException(
                $"Empty/null email payload for notification {notification.Id}");
        }

        if (string.IsNullOrWhiteSpace(notification.RecipientEmail))
        {
            throw new NonRetriableNotificationException(
                $"No recipient email set for notification {notification.Id}");
        }

        var to = notification.RecipientEmail;
        var from = await ResolveFromAddressAsync(notification.OrganizationId, cancellationToken);

        var message = new EmailMessage(
            From: from,
            To: to,
            Subject: payload.Subject,
            Html: $"<p>{payload.Body}</p>",
            Text: payload.Body
        );

        var emailId = await _emailProvider.SendAsync(message, cancellationToken);

        _logger.LogInformation(
            "Email sent via Resend for notification {Id} to {To} from {From}",
            notification.Id, to, from);

        await WriteLogAsync(
            notification,
            "Resend",
            $"accepted: id={emailId}",
            isSuccess: true,
            cancellationToken);

        return true;
    }

    // Every Notification row is created by CampaignWorker (see architecture notes —
    // Import/other flows never create Notification rows directly), so this is always
    // an org-branded campaign send. The display name is per-org (Organization.FromName);
    // the email address stays on our verified sending domain since SES only lets us
    // send from identities we've verified — orgs can't bring their own arbitrary From
    // address without a full domain-verification flow, which doesn't exist yet.
    private async Task<string> ResolveFromAddressAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        var org = await _context.Organizations
            .Where(o => o.Id == organizationId)
            .Select(o => new { o.FromName, o.FromEmail })
            .FirstOrDefaultAsync(cancellationToken);

        var fromEmail = string.IsNullOrWhiteSpace(org?.FromEmail)
            ? CampaignFromEmail
            : org.FromEmail;

        if (string.IsNullOrWhiteSpace(org?.FromName))
        {
            _logger.LogWarning(
                "Organization {OrgId} has no FromName configured — falling back to NotificationHub default",
                organizationId);
            return $"NotificationHub <{fromEmail}>";
        }

        return $"{org.FromName} <{fromEmail}>";
    }

    private async Task WriteLogAsync(
        Notification notification,
        string provider,
        string response,
        bool isSuccess,
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
                IsSuccess = isSuccess,
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