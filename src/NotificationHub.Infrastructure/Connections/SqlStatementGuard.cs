using System.Text.RegularExpressions;

namespace NotificationHub.Infrastructure.Connections;

public class ReadOnlyViolationException : Exception
{
    public ReadOnlyViolationException(string message) : base(message) { }
}

public static class SqlStatementGuard
{
    private static readonly string[] ForbiddenKeywords =
    {
        "INSERT", "UPDATE", "DELETE", "DROP", "ALTER",
        "TRUNCATE", "CREATE", "GRANT", "REVOKE", "EXEC",
        "EXECUTE", "MERGE", "REPLACE", "CALL"
    };

    public static void EnsureReadOnly(string sql)
    {
        var trimmed = sql.TrimStart();

        if (!trimmed.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
        {
            throw new ReadOnlyViolationException(
                "Only SELECT statements are permitted against connected data sources.");
        }

        foreach (var keyword in ForbiddenKeywords)
        {
            if (Regex.IsMatch(sql, $@"\b{keyword}\b", RegexOptions.IgnoreCase))
            {
                throw new ReadOnlyViolationException(
                    $"Statement contains forbidden keyword '{keyword}'.");
            }
        }
    }
}