namespace NotificationHub.Application.Validation;

/// <summary>
/// Email-address sanity checks shared by the notification validator and the
/// controllers that accept a reply-to/sender address directly.
/// </summary>
public static class EmailAddressRules
{
    /// <summary>
    /// True when the value is exactly one bare email address — no display
    /// name ("Support &lt;a@b.c&gt;"), no comma/semicolon separated list.
    /// Providers take reply-to as a single address, so anything richer is
    /// rejected at the edge instead of failing at the provider.
    /// </summary>
    public static bool IsSingleEmailAddress(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        var trimmed = value.Trim();

        // Multiple addresses in one string.
        if (trimmed.Contains(',') || trimmed.Contains(';'))
            return false;

        try
        {
            var address = new System.Net.Mail.MailAddress(trimmed);

            // MailAddress accepts "Display Name <a@b.c>" — the parsed address
            // then differs from the input, which is what we reject here.
            return string.Equals(address.Address, trimmed, StringComparison.OrdinalIgnoreCase);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    /// <summary>
    /// Lowercased domain part of an address, or null when it cannot be parsed.
    /// </summary>
    public static string? TryGetDomain(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        try
        {
            var address = new System.Net.Mail.MailAddress(value.Trim());
            var at = address.Address.LastIndexOf('@');
            return at <= 0 ? null : address.Address[(at + 1)..].Trim().ToLowerInvariant();
        }
        catch (FormatException)
        {
            return null;
        }
    }
}
