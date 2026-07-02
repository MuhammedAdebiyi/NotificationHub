using NotificationHub.Infrastructure.Auth;

namespace NotificationHub.Infrastructure.Auth;

public static class ApiKeyGenerator
{
    public static string Generate()
    {
        var token = RandomTokenGenerator.Generate(32);
        return $"nhub_live_{token}";
    }

    public static string Hash(string apiKey) =>
        BCrypt.Net.BCrypt.HashPassword(apiKey, workFactor: 10);

    public static bool Verify(string apiKey, string hash) =>
        BCrypt.Net.BCrypt.Verify(apiKey, hash);

    public static string GetPrefix(string apiKey) =>
        apiKey[..12];
}