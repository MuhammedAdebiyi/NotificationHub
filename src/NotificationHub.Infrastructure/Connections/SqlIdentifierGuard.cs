using System.Text.RegularExpressions;

namespace NotificationHub.Infrastructure.Connections;

public static class SqlIdentifierGuard
{
    private static readonly Regex ValidIdentifier = new(@"^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.Compiled);

    public static void EnsureValid(string identifier, string fieldName)
    {
        if (!ValidIdentifier.IsMatch(identifier))
        {
            throw new InvalidOperationException(
                $"{fieldName} '{identifier}' is not a valid identifier.");
        }
    }

    public static string QuotePostgres(string identifier) => $"\"{identifier}\"";
    public static string QuoteMySql(string identifier) => $"`{identifier}`";
    public static string QuoteSqlServer(string identifier) => $"[{identifier}]";
}