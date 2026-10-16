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
    private const string CampaignFromEmail = "notifications@mail.notificationhub.space";

    private readonly IEmailProviderFactory _emailProviderFactory;
    private readonly ILogger<StubNotificationProvider> _logger;
    private readonly AppDbContext _context;
    private readonly IUnsubscribeTokenService _unsubscribeTokens;
    private readonly Microsoft.Extensions.Configuration.IConfiguration _configuration;

    public string? LastProviderType { get; private set; }

    public string? LastMessageId { get; private set; }

    public StubNotificationProvider(
        IEmailProviderFactory emailProviderFactory,
        ILogger<StubNotificationProvider> logger,
        AppDbContext context,
        IUnsubscribeTokenService unsubscribeTokens,
        Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        _emailProviderFactory = emailProviderFactory;
        _logger = logger;
        _context = context;
        _unsubscribeTokens = unsubscribeTokens;
        _configuration = configuration;
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
            await WriteLogAsync(notification, LastProviderType ?? "Unknown", $"error: {ex.Message}", isSuccess: false, cancellationToken);
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
        var normalizedTo = to.Trim().ToLowerInvariant();

        // Opted-out recipients are skipped entirely — required for unsubscribe
        // compliance and it keeps Gmail from re-classifying us as a bulk spammer.
        var optedOut = await _context.RecipientOptOuts.AnyAsync(
            o => o.OrganizationId == notification.OrganizationId && o.Email == normalizedTo,
            cancellationToken);

        if (optedOut)
        {
            _logger.LogInformation("Skipping notification {Id} — {To} unsubscribed", notification.Id, normalizedTo);
            await WriteLogAsync(notification, "System", "skipped — recipient unsubscribed", isSuccess: true, cancellationToken);
            return true;
        }

        var from = await ResolveFromAddressAsync(notification.OrganizationId, cancellationToken);
        var trackingPixel = $"<img src=\"https://notificationhub.space/api/v1/track/open/{notification.Id}\" width=\"1\" height=\"1\" style=\"display:none\" alt=\"\" />";

        var apiBase = (_configuration["App:ApiBaseUrl"] ?? "https://api.notificationhub.space").TrimEnd('/');
        var unsubscribeUrl = $"{apiBase}/api/v1/unsubscribe/{_unsubscribeTokens.CreateToken(notification.OrganizationId, normalizedTo)}";
        var unsubscribeFooter = $"<p style=\"margin-top:20px;font-size:12px;color:#999\"><a href=\"{unsubscribeUrl}\" style=\"color:#999\">Unsubscribe</a></p>";

        // Payloads arrive as {subject, body} plain text or {subject, html} full HTML
        // documents. Wrap only plain text in <p>; HTML goes through as-is so the
        // recipient actually renders the real email.
        var content = payload.Html ?? payload.Text ?? payload.Body ?? string.Empty;

        // Templates can carry a {{unsubscribe_url}} placeholder — substitute the
        // real per-recipient link so it doesn't render as broken literal text.
        var hadPlaceholder = false;
        foreach (var placeholder in new[] { "{{unsubscribe_url}}", "{unsubscribe_url}" })
        {
            if (content.Contains(placeholder, StringComparison.OrdinalIgnoreCase))
            {
                content = content.Replace(placeholder, unsubscribeUrl, StringComparison.OrdinalIgnoreCase);
                hadPlaceholder = true;
            }
        }

        var looksLikeHtml = content.Contains('<') && content.Contains('>');

        // If the template already has its own unsubscribe link (placeholder or
        // hand-written), keep the designer's footer and skip the auto one.
        var hasOwnUnsubscribe = hadPlaceholder ||
            content.Contains("/unsubscribe", StringComparison.OrdinalIgnoreCase);
        var footerHtml = hasOwnUnsubscribe ? string.Empty : unsubscribeFooter;

        string html;
        if (!looksLikeHtml)
        {
            html = $"<p>{content}</p>{footerHtml}{trackingPixel}";
        }
        else if (content.Contains("</body>", StringComparison.OrdinalIgnoreCase))
        {
            html = content.Replace("</body>", $"{footerHtml}{trackingPixel}</body>", StringComparison.OrdinalIgnoreCase);
        }
        else
        {
            html = content + footerHtml + trackingPixel;
        }

        var textPart = content + "\n\nUnsubscribe: " + unsubscribeUrl;

        var message = new EmailMessage(
            From: from,
            To: to,
            Subject: payload.Subject,
            Html: html,
            Text: textPart,
            UnsubscribeUrl: unsubscribeUrl
        );

        // Fallback chain: default provider first, then the rest. Individual
        // failures are written as their own log rows so the timeline shows
        // every attempt, then the winning provider on the success row.
        ProviderSendResult result;
        try
        {
            result = await _emailProviderFactory.SendWithFallbackAsync(
                notification.OrganizationId, message, cancellationToken);
        }
        catch (ProviderSendFailedException pex)
        {
            _logger.LogError(pex, "All providers failed for notification {Id}", notification.Id);

            foreach (var failure in pex.Failures)
            {
                await WriteLogAsync(notification, failure.ProviderType, $"error: {failure.Error}", isSuccess: false, cancellationToken);
            }

            LastProviderType = pex.Failures.Count > 0 ? pex.Failures[^1].ProviderType : "Unknown";
            return false;
        }

        var providerName = result.ProviderType;
        LastProviderType = providerName;
        LastMessageId = result.MessageId;

        foreach (var failure in result.Failures)
        {
            await WriteLogAsync(notification, failure.ProviderType, $"error: {failure.Error}", isSuccess: false, cancellationToken);
        }

        _logger.LogInformation(
            "Email sent via {Provider} for notification {Id} to {To} from {From}",
            providerName, notification.Id, to, from);

        await WriteLogAsync(
            notification,
            providerName,
            $"accepted: id={result.MessageId}",
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
        [property: JsonPropertyName("body")] string? Body,
        [property: JsonPropertyName("html")] string? Html,
        [property: JsonPropertyName("text")] string? Text
    );
}