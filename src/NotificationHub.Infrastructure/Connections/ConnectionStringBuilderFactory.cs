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
        var builder = new NpgsqlConnectionStringBuilder(raw)
        {
            Timeout = ConnectTimeoutSeconds,
            CommandTimeout = CommandTimeoutSeconds
        };
        return builder.ConnectionString;
    }

    private static string BuildMySql(string raw)
    {
        var builder = new MySqlConnectionStringBuilder(raw)
        {
            ConnectionTimeout = ConnectTimeoutSeconds,
            DefaultCommandTimeout = CommandTimeoutSeconds
        };
        return builder.ConnectionString;
    }

    private static string BuildSqlServer(string raw)
    {
        var builder = new SqlConnectionStringBuilder(raw)
        {
            ConnectTimeout = ConnectTimeoutSeconds
            // SqlCommand.CommandTimeout is set per-command, not on the builder — applied below
        };
        return builder.ConnectionString;
    }
}