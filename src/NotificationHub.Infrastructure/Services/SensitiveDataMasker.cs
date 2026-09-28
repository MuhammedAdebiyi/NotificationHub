using System.Text.RegularExpressions;

namespace NotificationHub.Infrastructure.Services;

/// <summary>
/// Redacts secret-looking values before payloads or provider responses leave
/// the API. The database keeps the original content (retries must send the
/// exact message) — masking happens only at the presentation boundary.
/// </summary>
public static partial class SensitiveDataMasker
{
    // Query-string style: ?token=..., &reset_token=..., including JSON-escaped
    // \u0026 / \u003F variants that appear inside stored HTML payloads.
    [GeneratedRegex(
        @"((?:\?|&|\\u0026|\\u003F)(?:token|code|otp|reset_token|resetToken|verify_token|verifyToken|verification_token|verificationToken|key|apikey|api_key|apiKey|secret|signature|auth|password|pwd|invite)=)[^&\s""'<>\\]+",
        RegexOptions.IgnoreCase)]
    private static partial Regex QueryParamRegex();

    // JSON-style: "reset_token":"abc..." inside the stored payload object.
    [GeneratedRegex(
        @"""(reset_token|resetToken|verify_token|verifyToken|verification_token|verificationToken|token|code|otp|secret|password|api_key|apiKey|apikey)""\s*:\s*""[^""]*""",
        RegexOptions.IgnoreCase)]
    private static partial Regex JsonFieldRegex();

    public static string Mask(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return value ?? string.Empty;

        var masked = QueryParamRegex().Replace(value, "$1***");
        masked = JsonFieldRegex().Replace(masked, match =>
            $"{match.Value[..(match.Value.LastIndexOf(':') + 1)]}\"***\"");
        return masked;
    }
}
