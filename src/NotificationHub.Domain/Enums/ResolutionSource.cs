namespace NotificationHub.Domain.Enums;

/// <summary>
/// Where a resolved address (reply-to or sender) came from. Persisted on the
/// notification at enqueue time so the dashboard can explain the effective
/// value, and so retries/DLQ replays never re-resolve it against settings
/// that may have changed since the request was accepted.
/// </summary>
public enum ResolutionSource
{
    /// <summary>Nothing configured anywhere — the provider's own default applies.</summary>
    ProviderDefault = 0,

    /// <summary>Supplied in the API request that created the notification.</summary>
    Request = 1,

    /// <summary>From the template the notification was created from.</summary>
    Template = 2,

    /// <summary>From organization settings (org reply-to / org sender).</summary>
    Organization = 3,

    /// <summary>
    /// Sender fallback: no reply-to was configured anywhere, so replies go to
    /// the organization's sender address.
    /// </summary>
    Sender = 4,
}

public static class ResolutionSourceExtensions
{
    /// <summary>Stable lowercase name used in API responses and the dashboard.</summary>
    public static string ToApiString(this ResolutionSource source) => source switch
    {
        ResolutionSource.Request => "request",
        ResolutionSource.Template => "template",
        ResolutionSource.Organization => "organization",
        ResolutionSource.Sender => "sender",
        _ => "default",
    };
}
