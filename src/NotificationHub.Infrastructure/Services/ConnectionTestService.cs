using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using MySqlConnector;
using Npgsql;
using NotificationHub.Application.Abstractions;
using NotificationHub.Domain.Enums;
using NotificationHub.Infrastructure.Connections;

namespace NotificationHub.Infrastructure.Services;

public class ConnectionTestService : IConnectionTestService
{
    private const string ProbeQuery = "SELECT 1";
    private const int CommandTimeoutSeconds = 30;
    private const string GenericFailureMessage =
        "Unable to connect to the database. Please verify your connection settings.";

    private readonly ILogger<ConnectionTestService> _logger;

    public ConnectionTestService(ILogger<ConnectionTestService> logger)
    {
        _logger = logger;
    }

    public async Task<ConnectionTestResult> TestConnectionAsync(
        Guid dataSourceId,
        DataSourceType type,
        string connectionString,
        CancellationToken cancellationToken = default)
    {
        SqlStatementGuard.EnsureReadOnly(ProbeQuery);

        var protocol = type.ToSqlProtocol();
        if (protocol is null)
        {
            // Not a driver/credential failure — safe to be specific, no secrets involved
            return new ConnectionTestResult(
                false,
                $"{type} is not a SQL data source and cannot be tested via connection string.");
        }

        try
        {
            var builtConnectionString = ConnectionStringBuilderFactory.Build(protocol.Value, connectionString);
            await ExecuteProbeAsync(protocol.Value, builtConnectionString, cancellationToken);

            return new ConnectionTestResult(true, "Connection successful.");
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Connection validation failed for DataSource {DataSourceId} ({Type})",
                dataSourceId, type);

            return new ConnectionTestResult(false, GenericFailureMessage);
        }
    }

    private static async Task ExecuteProbeAsync(
        SqlProtocol protocol,
        string connectionString,
        CancellationToken cancellationToken)
    {
        switch (protocol)
        {
            case SqlProtocol.Postgres:
                await using (var conn = new NpgsqlConnection(connectionString))
                {
                    await conn.OpenAsync(cancellationToken);
                    await using var cmd = new NpgsqlCommand(ProbeQuery, conn)
                    {
                        CommandTimeout = CommandTimeoutSeconds
                    };
                    await cmd.ExecuteScalarAsync(cancellationToken);
                }
                break;

            case SqlProtocol.MySql:
                await using (var conn = new MySqlConnection(connectionString))
                {
                    await conn.OpenAsync(cancellationToken);
                    await using var cmd = new MySqlCommand(ProbeQuery, conn)
                    {
                        CommandTimeout = CommandTimeoutSeconds
                    };
                    await cmd.ExecuteScalarAsync(cancellationToken);
                }
                break;

            case SqlProtocol.SqlServer:
                await using (var conn = new SqlConnection(connectionString))
                {
                    await conn.OpenAsync(cancellationToken);
                    await using var cmd = new SqlCommand(ProbeQuery, conn)
                    {
                        CommandTimeout = CommandTimeoutSeconds
                    };
                    await cmd.ExecuteScalarAsync(cancellationToken);
                }
                break;
        }
    }
}