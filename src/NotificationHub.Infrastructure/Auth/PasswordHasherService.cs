using NotificationHub.Application.Abstractions;

namespace NotificationHub.Infrastructure.Auth;

public class PasswordHasherService : IPasswordHasher
{
    public const int WorkFactor = 10;

    public string Hash(string password) =>
        BCrypt.Net.BCrypt.HashPassword(password, workFactor: WorkFactor);

    public bool Verify(string password, string hash) =>
        BCrypt.Net.BCrypt.Verify(password, hash);

    public bool NeedsRehash(string hash)
    {
        // bcrypt format: $2a$10$... — cost factor is the 2 chars after the second '$'
        if (string.IsNullOrEmpty(hash)
            || !hash.StartsWith("$2", StringComparison.Ordinal)
            || hash.Length < 6)
            return false;

        return !int.TryParse(hash.AsSpan(4, 2), out var rounds) || rounds != WorkFactor;
    }
}
