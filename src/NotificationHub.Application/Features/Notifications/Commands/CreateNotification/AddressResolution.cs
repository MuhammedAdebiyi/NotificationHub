using NotificationHub.Domain.Enums;

namespace NotificationHub.Application.Features.Notifications.Commands.CreateNotification;

/// <summary>Reply-to resolved for a single notification.</summary>
public record ResolvedReplyTo(string? Address, ResolutionSource Source);

/// <summary>Sender identity resolved for a single notification.</summary>
public record ResolvedSender(string? Email, string? Name, ResolutionSource Source);

/// <summary>
/// One-shot reply-to/sender resolution. It runs while the notification is
/// being enqueued and the result is persisted on the notification row, so
/// retries and DLQ replays reuse the value that was accepted with the
/// request instead of re-reading settings that may have changed since.
/// </summary>
public static class AddressResolution
{
    /// <summary>
    /// request.replyTo → template.replyTo → organization reply-to →
    /// organization sender email → provider default (null).
    /// </summary>
    public static ResolvedReplyTo ResolveReplyTo(
        string? requestReplyTo,
        string? templateReplyTo,
        string? organizationReplyTo,
        string? organizationSenderEmail)
    {
        if (!string.IsNullOrWhiteSpace(requestReplyTo))
            return new ResolvedReplyTo(requestReplyTo.Trim(), ResolutionSource.Request);

        if (!string.IsNullOrWhiteSpace(templateReplyTo))
            return new ResolvedReplyTo(templateReplyTo.Trim(), ResolutionSource.Template);

        if (!string.IsNullOrWhiteSpace(organizationReplyTo))
            return new ResolvedReplyTo(organizationReplyTo.Trim(), ResolutionSource.Organization);

        if (!string.IsNullOrWhiteSpace(organizationSenderEmail))
            return new ResolvedReplyTo(organizationSenderEmail.Trim(), ResolutionSource.Sender);

        return new ResolvedReplyTo(null, ResolutionSource.ProviderDefault);
    }

    /// <summary>
    /// senderEmail/senderName resolve independently but share the same
    /// precedence: request → template → organization → provider default.
    /// The reported source is the highest-precedence level that contributed
    /// either part of the sender identity.
    /// </summary>
    public static ResolvedSender ResolveSender(
        string? requestEmail,
        string? requestName,
        string? templateEmail,
        string? templateName,
        string? organizationEmail,
        string? organizationName)
    {
        var email = First(requestEmail, templateEmail, organizationEmail);
        var name = First(requestName, templateName, organizationName);

        var source = !string.IsNullOrWhiteSpace(requestEmail) || !string.IsNullOrWhiteSpace(requestName)
            ? ResolutionSource.Request
            : !string.IsNullOrWhiteSpace(templateEmail) || !string.IsNullOrWhiteSpace(templateName)
                ? ResolutionSource.Template
                : !string.IsNullOrWhiteSpace(organizationEmail) || !string.IsNullOrWhiteSpace(organizationName)
                    ? ResolutionSource.Organization
                    : ResolutionSource.ProviderDefault;

        return new ResolvedSender(email, name, source);
    }

    private static string? First(params string?[] values) =>
        values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v))?.Trim();
}
