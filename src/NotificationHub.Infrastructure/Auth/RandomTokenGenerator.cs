using System.Security.Cryptography;

namespace NotificationHub.Infrastructure.Auth;

public static class RandomTokenGenerator
{
    public static string Generate(int length = 32)
    {
        // Base64 encodes 3 bytes -> 4 chars, so over-request bytes to guarantee enough chars after stripping
        var byteCount = (int)Math.Ceiling(length / 4.0 * 3.0) + 3;
        var randomBytes = RandomNumberGenerator.GetBytes(byteCount);

        var token = Convert.ToBase64String(randomBytes)
            .Replace("+", "")
            .Replace("/", "")
            .Replace("=", "");

        return token[..length];
    }
}