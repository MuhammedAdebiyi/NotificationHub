namespace NotificationHub.Infrastructure.Auth;

public static class PasswordHasher
{
    public static string Hash(string password) =>
        BCrypt.Net.BCrypt.HashPassword(password, workFactor: PasswordHasherService.WorkFactor);

    public static bool Verify(string password, string hash) =>
        BCrypt.Net.BCrypt.Verify(password, hash);
}