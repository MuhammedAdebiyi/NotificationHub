namespace NotificationHub.Application.Validation;

/// <summary>
/// Stable, machine-readable failure codes returned by the API. Clients should
/// branch on these instead of matching message text.
/// </summary>
public static class NotificationErrorCodes
{
    /// <summary>replyTo/senderEmail is not a single valid email address.</summary>
    public const string InvalidReplyTo = "invalid_reply_to";
    public const string InvalidSenderEmail = "invalid_sender_email";
    public const string InvalidSenderName = "invalid_sender_name";

    /// <summary>The address domain is not verified with any configured provider.</summary>
    public const string UnverifiedReplyToDomain = "unverified_reply_to_domain";
    public const string UnverifiedSenderDomain = "unverified_sender_domain";

    /// <summary>templateId does not resolve to a template in this organization.</summary>
    public const string TemplateNotFound = "template_not_found";

    /// <summary>The API key's organization no longer exists.</summary>
    public const string OrganizationNotFound = "organization_not_found";
}
