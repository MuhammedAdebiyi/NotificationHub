using System.Security.Cryptography;
using System.Text;

namespace NotificationHub.Infrastructure.Webhooks;

/// <summary>
/// Verifies SendByte's `sendbyte-signature` header:
/// `t=<unix>,v1=<hex hmac sha256>` where the HMAC covers `"<t>.<raw body>"`.
/// </summary>
public static class SendByteSignatureVerifier
{
    public static bool Verify(
        string? secret,
        string? signatureHeader,
        string rawBody,
        int toleranceSeconds = 300)
    {
        if (string.IsNullOrWhiteSpace(secret)
            || string.IsNullOrWhiteSpace(signatureHeader)
            || string.IsNullOrEmpty(rawBody))
            return false;

        string? timestamp = null;
        string? signature = null;
        foreach (var pair in signatureHeader.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            var kv = pair.Split('=', 2);
            if (kv.Length != 2) continue;
            if (kv[0].Trim() == "t") timestamp = kv[1].Trim();
            else if (kv[0].Trim() == "v1") signature = kv[1].Trim();
        }

        if (timestamp is null || signature is null)
            return false;

        if (!long.TryParse(timestamp, out var unixSeconds))
            return false;

        var age = Math.Abs(DateTimeOffset.UtcNow.ToUnixTimeSeconds() - unixSeconds);
        if (age > toleranceSeconds)
            return false;

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var computed = Convert.ToHexString(
            hmac.ComputeHash(Encoding.UTF8.GetBytes($"{timestamp}.{rawBody}")));

        return CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(computed.ToUpperInvariant()),
            Encoding.ASCII.GetBytes(signature.ToUpperInvariant()));
    }
}
