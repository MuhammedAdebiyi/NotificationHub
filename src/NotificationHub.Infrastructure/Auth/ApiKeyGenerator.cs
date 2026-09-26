using System.Security.Cryptography;
using System.Text;

namespace NotificationHub.Infrastructure.Auth;

public static class ApiKeyGenerator
{
    public static string Generate()
    {
        var token = RandomTokenGenerator.Generate(32);
        return $"nhub_live_{token}";
    }

    public static string Hash(string apiKey) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(apiKey)));

    public static bool IsLegacyBcrypt(string hash) =>
        hash.StartsWith("$2", StringComparison.Ordinal);

    public static bool Verify(string apiKey, string hash)
    {
        if (IsLegacyBcrypt(hash))
            return BCrypt.Net.BCrypt.Verify(apiKey, hash);

        byte[] stored;
        try
        {
            stored = Convert.FromHexString(hash);
        }
        catch (FormatException)
        {
            return false;
        }

        var computed = SHA256.HashData(Encoding.UTF8.GetBytes(apiKey));
        return CryptographicOperations.FixedTimeEquals(stored, computed);
    }

    public static string GetPrefix(string apiKey) =>
        apiKey[..12];
}
