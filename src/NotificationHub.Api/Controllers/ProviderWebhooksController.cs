using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NotificationHub.Application.Abstractions;
using NotificationHub.Infrastructure.Webhooks;

namespace NotificationHub.Api.Controllers;

/// <summary>
/// Inbound delivery-event webhooks from providers (SendByte). Public endpoint:
/// authenticity comes from the HMAC signature, not from JWT/API-key auth.
/// </summary>
[ApiController]
[Route("api/v1/webhooks/provider")]
public class ProviderWebhooksController : ControllerBase
{
    private readonly INotificationService _notificationService;
    private readonly IWebhookService _webhookService;
    private readonly IConfiguration _config;
    private readonly ILogger<ProviderWebhooksController> _logger;

    public ProviderWebhooksController(
        INotificationService notificationService,
        IWebhookService webhookService,
        IConfiguration config,
        ILogger<ProviderWebhooksController> logger)
    {
        _notificationService = notificationService;
        _webhookService = webhookService;
        _config = config;
        _logger = logger;
    }

    [HttpPost("sendbyte")]
    [AllowAnonymous]
    public async Task<IActionResult> ReceiveSendByteEvent(CancellationToken cancellationToken)
    {
        string rawBody;
        using (var reader = new StreamReader(Request.Body))
        {
            rawBody = await reader.ReadToEndAsync(cancellationToken);
        }

        var secret = _config["SendByte:WebhookSecret"];
        var signature = Request.Headers["sendbyte-signature"].FirstOrDefault();

        if (!SendByteSignatureVerifier.Verify(secret, signature, rawBody))
        {
            _logger.LogWarning(
                "Rejected SendByte webhook (secret configured: {HasSecret}, signature present: {HasSig})",
                !string.IsNullOrWhiteSpace(secret), signature is not null);
            return Unauthorized(new { error = "Invalid signature." });
        }

        string? type;
        string? emailId;
        string? reason;
        try
        {
            using var doc = JsonDocument.Parse(rawBody);
            type = doc.RootElement.TryGetProperty("type", out var t) ? t.GetString() : null;
            if (doc.RootElement.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Object)
            {
                emailId = data.TryGetProperty("email_id", out var id) ? id.GetString() : null;
                if (data.TryGetProperty("smtp_response", out var smtp)) reason = smtp.GetString();
                else if (data.TryGetProperty("reason", out var r)) reason = r.GetString();
                else if (data.TryGetProperty("bounce_type", out var b)) reason = b.GetString();
                else reason = null;
            }
            else
            {
                emailId = null;
                reason = null;
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Malformed SendByte webhook body");
            return BadRequest(new { error = "Malformed payload." });
        }

        if (string.IsNullOrEmpty(type) || string.IsNullOrEmpty(emailId))
        {
            // domain.* events and anything without an email_id — acknowledge only.
            _logger.LogInformation("SendByte webhook acknowledged without action: {Type}", type);
            return Ok(new { received = true });
        }

        var outcome = await _notificationService.ApplyProviderEventAsync(
            emailId, type, reason, cancellationToken);

        if (outcome is null)
        {
            // Auth emails (verification/reset) send via SendByte but never become
            // notification rows — their events are expected and simply logged.
            _logger.LogInformation(
                "SendByte event {Type} for {EmailId}: no matching notification", type, emailId);
            return Ok(new { received = true });
        }

        if (outcome.Changed)
        {
            _logger.LogInformation(
                "Notification {PublicId} transitioned to {Status} via SendByte event {Type}",
                outcome.PublicId, outcome.Status, type);

            if (outcome.Status == "Delivered")
            {
                await _webhookService.DispatchAsync(
                    "notification.delivered",
                    outcome.OrganizationId,
                    new { outcome.PublicId, provider = "SendByte", type });
            }
            else if (outcome.Status == "Bounced")
            {
                await _webhookService.DispatchAsync(
                    "notification.bounced",
                    outcome.OrganizationId,
                    new { outcome.PublicId, provider = "SendByte", type, reason });
            }
        }

        return Ok(new { received = true, status = outcome.Status });
    }
}
