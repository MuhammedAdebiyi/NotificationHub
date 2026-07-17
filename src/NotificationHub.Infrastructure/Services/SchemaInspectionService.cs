using Microsoft.Data.SqlClient;
using MySqlConnector;
using Npgsql;
using NotificationHub.Application.Abstractions;
using NotificationHub.Domain.Enums;
using NotificationHub.Infrastructure.Connections;

namespace NotificationHub.Infrastructure.Services;

public class SchemaInspectionService : ISchemaInspectionService
{
    public async Task<List<string>> GetTablesAsync(
        DataSourceType type, string connectionString, CancellationToken cancellationToken = default)
    {
        var protocol = type.ToSqlProtocol()
            ?? throw new InvalidOperationException($"{type} does not support schema inspection.");

        var builtConnectionString = ConnectionStringBuilderFactory.Build(protocol, connectionString);
        var tables = new List<string>();

        switch (protocol)
        {
            case SqlProtocol.Postgres:
            {
                const string sql = """
                    SELECT table_name FROM information_schema.tables
                    WHERE table_schema NOT IN ('pg_catalog', 'information_schema')
                    AND table_type = 'BASE TABLE'
                    ORDER BY table_name
                    """;
                SqlStatementGuard.EnsureReadOnly(sql);

                await using var conn = new NpgsqlConnection(builtConnectionString);
                await conn.OpenAsync(cancellationToken);
                await using var cmd = new NpgsqlCommand(sql, conn);
                await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                    tables.Add(reader.GetString(0));
                break;
            }

            case SqlProtocol.MySql:
            {
                const string sql = """
                    SELECT table_name FROM information_schema.tables
                    WHERE table_schema = DATABASE()
                    AND table_type = 'BASE TABLE'
                    ORDER BY table_name
                    """;
                SqlStatementGuard.EnsureReadOnly(sql);

                await using var conn = new MySqlConnection(builtConnectionString);
                await conn.OpenAsync(cancellationToken);
                await using var cmd = new MySqlCommand(sql, conn);
                await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                    tables.Add(reader.GetString(0));
                break;
            }

            case SqlProtocol.SqlServer:
            {
                const string sql = """
                    SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES
                    WHERE TABLE_TYPE = 'BASE TABLE'
                    ORDER BY TABLE_NAME
                    """;
                SqlStatementGuard.EnsureReadOnly(sql);

                await using var conn = new SqlConnection(builtConnectionString);
                await conn.OpenAsync(cancellationToken);
                await using var cmd = new SqlCommand(sql, conn);
                await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                    tables.Add(reader.GetString(0));
                break;
            }
        }

        return tables;
    }

    public async Task<List<ColumnInfo>> GetColumnsAsync(
        DataSourceType type, string connectionString, string tableName, CancellationToken cancellationToken = default)
    {
        var protocol = type.ToSqlProtocol()
            ?? throw new InvalidOperationException($"{type} does not support schema inspection.");

        var builtConnectionString = ConnectionStringBuilderFactory.Build(protocol, connectionString);
        var columns = new List<ColumnInfo>();

        switch (protocol)
        {
            case SqlProtocol.Postgres:
            {
                const string sql = """
                    SELECT column_name, data_type, is_nullable
                    FROM information_schema.columns
                    WHERE table_name = @tableName
                    ORDER BY ordinal_position
                    """;
                SqlStatementGuard.EnsureReadOnly(sql);

                await using var conn = new NpgsqlConnection(builtConnectionString);
                await conn.OpenAsync(cancellationToken);
                await using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("tableName", tableName);
                await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                    columns.Add(new ColumnInfo(reader.GetString(0), reader.GetString(1), reader.GetString(2) == "YES"));
                break;
            }

            case SqlProtocol.MySql:
            {
                const string sql = """
                    SELECT column_name, data_type, is_nullable
                    FROM information_schema.columns
                    WHERE table_schema = DATABASE() AND table_name = @tableName
                    ORDER BY ordinal_position
                    """;
                SqlStatementGuard.EnsureReadOnly(sql);

                await using var conn = new MySqlConnection(builtConnectionString);
                await conn.OpenAsync(cancellationToken);
                await using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@tableName", tableName);
                await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                    columns.Add(new ColumnInfo(reader.GetString(0), reader.GetString(1), reader.GetString(2) == "YES"));
                break;
            }

            case SqlProtocol.SqlServer:
            {
                const string sql = """
                    SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE
                    FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_NAME = @tableName
                    ORDER BY ORDINAL_POSITION
                    """;
                SqlStatementGuard.EnsureReadOnly(sql);

                await using var conn = new SqlConnection(builtConnectionString);
                await conn.OpenAsync(cancellationToken);
                await using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@tableName", tableName);
                await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                    columns.Add(new ColumnInfo(reader.GetString(0), reader.GetString(1), reader.GetString(2) == "YES"));
                break;
            }
        }

        return columns;
    }
}