using System.Data.Common;
using Microsoft.Data.SqlClient;
using MySqlConnector;
using Npgsql;
using NotificationHub.Application.Abstractions;
using NotificationHub.Domain.Enums;
using NotificationHub.Infrastructure.Connections;

namespace NotificationHub.Infrastructure.Services;

public class ImportDataReader : IImportDataReader
{
    public async Task<ImportBatchResult> ReadBatchAsync(
        DataSourceType type,
        string connectionString,
        string tableName,
        string primaryKeyColumn,
        string emailColumn,
        string? firstNameColumn,
        string? lastNameColumn,
        string? whereClause,
        string? lastCursor,
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        var protocol = type.ToSqlProtocol()
            ?? throw new InvalidOperationException($"{type} is not supported for row import.");

        SqlIdentifierGuard.EnsureValid(tableName, nameof(tableName));
        SqlIdentifierGuard.EnsureValid(primaryKeyColumn, nameof(primaryKeyColumn));
        SqlIdentifierGuard.EnsureValid(emailColumn, nameof(emailColumn));
        if (firstNameColumn is not null) SqlIdentifierGuard.EnsureValid(firstNameColumn, nameof(firstNameColumn));
        if (lastNameColumn is not null) SqlIdentifierGuard.EnsureValid(lastNameColumn, nameof(lastNameColumn));
        EnsureSafeWhereClause(whereClause);

        var builtConnectionString = ConnectionStringBuilderFactory.Build(protocol, connectionString);

        return protocol switch
        {
            SqlProtocol.Postgres => await ReadPostgresAsync(
                builtConnectionString, tableName, primaryKeyColumn, emailColumn,
                firstNameColumn, lastNameColumn, whereClause, lastCursor, batchSize, cancellationToken),
            SqlProtocol.MySql => await ReadMySqlAsync(
                builtConnectionString, tableName, primaryKeyColumn, emailColumn,
                firstNameColumn, lastNameColumn, whereClause, lastCursor, batchSize, cancellationToken),
            SqlProtocol.SqlServer => await ReadSqlServerAsync(
                builtConnectionString, tableName, primaryKeyColumn, emailColumn,
                firstNameColumn, lastNameColumn, whereClause, lastCursor, batchSize, cancellationToken),
            _ => throw new InvalidOperationException($"Unhandled protocol {protocol}")
        };
    }

    // WhereClause is user-supplied but never allowed to be arbitrary SQL — it's a single
    // boolean condition ANDed onto our own SELECT, not a statement. Block statement
    // terminators, comments, and the same forbidden keywords as everywhere else.
    private static void EnsureSafeWhereClause(string? whereClause)
    {
        if (string.IsNullOrWhiteSpace(whereClause)) return;

        if (whereClause.Contains(';') || whereClause.Contains("--") || whereClause.Contains("/*"))
        {
            throw new InvalidOperationException(
                "WhereClause may not contain statement terminators or comments.");
        }

        SqlStatementGuard.EnsureReadOnly($"SELECT 1 WHERE {whereClause}");
    }

    private static object? ParseCursor(string? cursor)
    {
        if (cursor is null) return null;
        if (long.TryParse(cursor, out var longVal)) return longVal;
        if (Guid.TryParse(cursor, out var guidVal)) return guidVal;
        throw new InvalidOperationException(
            $"Primary key cursor '{cursor}' is neither an integer nor a UUID. " +
            "This import pipeline only supports integer or UUID primary keys.");
    }

    private static string BuildSelectList(
        Func<string, string> quote, string pkCol, string emailCol, string? firstNameCol, string? lastNameCol)
    {
        var cols = new List<string> { quote(pkCol), quote(emailCol) };
        cols.Add(firstNameCol is not null ? quote(firstNameCol) : "NULL");
        cols.Add(lastNameCol is not null ? quote(lastNameCol) : "NULL");
        return string.Join(", ", cols);
    }

    private static string BuildWhere(string pkQuoted, string? lastCursor, string? whereClause)
    {
        var conditions = new List<string>();
        if (lastCursor is not null) conditions.Add($"{pkQuoted} > @cursor");
        if (!string.IsNullOrWhiteSpace(whereClause)) conditions.Add($"({whereClause})");
        return conditions.Count > 0 ? $"WHERE {string.Join(" AND ", conditions)}" : "";
    }

    private static async Task<ImportBatchResult> ReadPostgresAsync(
        string connectionString, string tableName, string pkCol, string emailCol,
        string? firstNameCol, string? lastNameCol, string? whereClause,
        string? lastCursor, int batchSize, CancellationToken cancellationToken)
    {
        var pk = SqlIdentifierGuard.QuotePostgres(pkCol);
        var table = SqlIdentifierGuard.QuotePostgres(tableName);
        var selectList = BuildSelectList(SqlIdentifierGuard.QuotePostgres, pkCol, emailCol, firstNameCol, lastNameCol);
        var where = BuildWhere(pk, lastCursor, whereClause);

        var sql = $"SELECT {selectList} FROM {table} {where} ORDER BY {pk} LIMIT @batchSize";
        SqlStatementGuard.EnsureReadOnly(sql);

        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(cancellationToken);
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("batchSize", batchSize);
        if (lastCursor is not null)
            cmd.Parameters.AddWithValue("cursor", ParseCursor(lastCursor)!);

        return await ReadRowsAsync(cmd, cancellationToken);
    }

    private static async Task<ImportBatchResult> ReadMySqlAsync(
        string connectionString, string tableName, string pkCol, string emailCol,
        string? firstNameCol, string? lastNameCol, string? whereClause,
        string? lastCursor, int batchSize, CancellationToken cancellationToken)
    {
        var pk = SqlIdentifierGuard.QuoteMySql(pkCol);
        var table = SqlIdentifierGuard.QuoteMySql(tableName);
        var selectList = BuildSelectList(SqlIdentifierGuard.QuoteMySql, pkCol, emailCol, firstNameCol, lastNameCol);
        var where = BuildWhere(pk, lastCursor, whereClause);

        var sql = $"SELECT {selectList} FROM {table} {where} ORDER BY {pk} LIMIT @batchSize";
        SqlStatementGuard.EnsureReadOnly(sql);

        await using var conn = new MySqlConnection(connectionString);
        await conn.OpenAsync(cancellationToken);
        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@batchSize", batchSize);
        if (lastCursor is not null)
            cmd.Parameters.AddWithValue("@cursor", ParseCursor(lastCursor)!);

        return await ReadRowsAsync(cmd, cancellationToken);
    }

    private static async Task<ImportBatchResult> ReadSqlServerAsync(
        string connectionString, string tableName, string pkCol, string emailCol,
        string? firstNameCol, string? lastNameCol, string? whereClause,
        string? lastCursor, int batchSize, CancellationToken cancellationToken)
    {
        var pk = SqlIdentifierGuard.QuoteSqlServer(pkCol);
        var table = SqlIdentifierGuard.QuoteSqlServer(tableName);
        var selectList = BuildSelectList(SqlIdentifierGuard.QuoteSqlServer, pkCol, emailCol, firstNameCol, lastNameCol);
        var where = BuildWhere(pk, lastCursor, whereClause);

        var sql = $"SELECT TOP (@batchSize) {selectList} FROM {table} {where} ORDER BY {pk}";
        SqlStatementGuard.EnsureReadOnly(sql);

        await using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync(cancellationToken);
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@batchSize", batchSize);
        if (lastCursor is not null)
            cmd.Parameters.AddWithValue("@cursor", ParseCursor(lastCursor)!);

        return await ReadRowsAsync(cmd, cancellationToken);
    }

    private static async Task<ImportBatchResult> ReadRowsAsync(DbCommand cmd, CancellationToken cancellationToken)
    {
        var rows = new List<ImportedRow>();
        string? lastCursor = null;

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var pkValue = reader.GetValue(0)?.ToString() ?? string.Empty;
            var email = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);
            var firstName = reader.IsDBNull(2) ? null : reader.GetString(2);
            var lastName = reader.IsDBNull(3) ? null : reader.GetString(3);

            rows.Add(new ImportedRow(pkValue, email, firstName, lastName));
            lastCursor = pkValue;
        }

        return new ImportBatchResult(rows, lastCursor);
    }
}