using NotificationHub.Domain.Enums;

namespace NotificationHub.Application.Abstractions;

public record ImportedRow(string PrimaryKeyValue, string Email, string? FirstName, string? LastName);
public record ImportBatchResult(List<ImportedRow> Rows, string? LastCursor);

public interface IImportDataReader
{
    Task<ImportBatchResult> ReadBatchAsync(
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
        CancellationToken cancellationToken = default);
}