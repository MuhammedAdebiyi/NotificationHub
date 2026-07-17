using Microsoft.Data.SqlClient;
using MySqlConnector;
using Npgsql;

namespace NotificationHub.Infrastructure.Connections;

public static class ConnectionStringBuilderFactory
{
    private const int ConnectTimeoutSeconds = 5;
    private const int CommandTimeoutSeconds = 30;

    public static string Build(SqlProtocol protocol, string rawConnectionString)
    {
        return protocol switch
        {
            SqlProtocol.Postgres => BuildPostgres(rawConnectionString),
            SqlProtocol.MySql => BuildMySql(rawConnectionString),
            SqlProtocol.SqlServer => BuildSqlServer(rawConnectionString),
            _ => throw new ArgumentOutOfRangeException(nameof(protocol))
        };
    }

    private static string BuildPostgres(string raw)
    {
        var cleaned = StripShellWrapper(raw);
        var normalized = IsUriStyle(cleaned) ? ConvertPostgresUriToKeywordString(cleaned) : cleaned;

        var builder = new NpgsqlConnectionStringBuilder(normalized)
        {
            Timeout = ConnectTimeoutSeconds,
            CommandTimeout = CommandTimeoutSeconds
        };
        return builder.ConnectionString;
    }

    private static string BuildMySql(string raw)
    {
        var cleaned = StripShellWrapper(raw);
        var normalized = IsUriStyle(cleaned) ? ConvertMySqlUriToKeywordString(cleaned) : cleaned;

        var builder = new MySqlConnectionStringBuilder(normalized)
        {
            ConnectionTimeout = ConnectTimeoutSeconds,
            DefaultCommandTimeout = CommandTimeoutSeconds
        };
        return builder.ConnectionString;
    }

    private static string BuildSqlServer(string raw)
    {
        // SQL Server connection strings from providers are already keyword-style
        // (Azure SQL, etc. don't hand out sqlserver:// URIs) — no conversion needed.
        var builder = new SqlConnectionStringBuilder(raw)
        {
            ConnectTimeout = ConnectTimeoutSeconds
        };
        return builder.ConnectionString;
    }

    private static bool IsUriStyle(string raw) =>
        raw.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) ||
        raw.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase) ||
        raw.StartsWith("mysql://", StringComparison.OrdinalIgnoreCase);

    // Users sometimes paste the "psql '<uri>'" shell one-liner instead of the bare
    // connection string (Neon/Supabase both offer this as a copy option in their
    // dashboards). Strip a leading psql/sql command name and surrounding quotes.
    private static string StripShellWrapper(string raw)
    {
        var trimmed = raw.Trim();

        if (trimmed.StartsWith("psql ", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("sql ", StringComparison.OrdinalIgnoreCase))
        {
            var firstQuote = trimmed.IndexOfAny(new[] { '\'', '"' });
            if (firstQuote >= 0)
            {
                var quoteChar = trimmed[firstQuote];
                var lastQuote = trimmed.LastIndexOf(quoteChar);
                if (lastQuote > firstQuote)
                    trimmed = trimmed[(firstQuote + 1)..lastQuote];
            }
        }
        else if (trimmed.Length >= 2 &&
                 (trimmed[0] == '\'' || trimmed[0] == '"') &&
                 trimmed[^1] == trimmed[0])
        {
            // Plain quoted string with no command prefix
            trimmed = trimmed[1..^1];
        }

        return trimmed;
    }

    private static string ConvertPostgresUriToKeywordString(string raw)
    {
        var uri = new Uri(raw);
        var userInfo = uri.UserInfo.Split(':', 2);
        var username = Uri.UnescapeDataString(userInfo[0]);
        var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "";
        var database = uri.AbsolutePath.TrimStart('/');

        var sslModeParam = GetQueryParam(uri, "sslmode") ?? "require";
        // Neon/Supabase send lowercase "require" — Npgsql's SslMode enum member is "Require"
        var sslModeName = char.ToUpperInvariant(sslModeParam[0]) + sslModeParam[1..];

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.Port > 0 ? uri.Port : 5432,
            Username = username,
            Password = password,
            Database = database,
            SslMode = Enum.TryParse<SslMode>(sslModeName, true, out var parsed) ? parsed : SslMode.Require,
        };

        return builder.ConnectionString;
    }

    private static string ConvertMySqlUriToKeywordString(string raw)
    {
        var uri = new Uri(raw);
        var userInfo = uri.UserInfo.Split(':', 2);
        var username = Uri.UnescapeDataString(userInfo[0]);
        var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "";
        var database = uri.AbsolutePath.TrimStart('/');

        var builder = new MySqlConnectionStringBuilder
        {
            Server = uri.Host,
            Port = (uint)(uri.Port > 0 ? uri.Port : 3306),
            UserID = username,
            Password = password,
            Database = database,
            SslMode = MySqlSslMode.Required, // PlanetScale requires TLS
        };

        return builder.ConnectionString;
    }

    private static string? GetQueryParam(Uri uri, string key)
    {
        var query = uri.Query.TrimStart('?');
        foreach (var pair in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var kv = pair.Split('=', 2);
            if (kv.Length == 2 && Uri.UnescapeDataString(kv[0]).Equals(key, StringComparison.OrdinalIgnoreCase))
                return Uri.UnescapeDataString(kv[1]);
        }
        return null;
    }
}