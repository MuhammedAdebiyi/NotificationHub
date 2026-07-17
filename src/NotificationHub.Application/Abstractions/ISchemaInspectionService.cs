using NotificationHub.Domain.Enums;

namespace NotificationHub.Application.Abstractions;

public record ColumnInfo(string Name, string DataType, bool IsNullable);

public interface ISchemaInspectionService
{
    Task<List<string>> GetTablesAsync(
        DataSourceType type, string connectionString, CancellationToken cancellationToken = default);

    Task<List<ColumnInfo>> GetColumnsAsync(
        DataSourceType type, string connectionString, string tableName, CancellationToken cancellationToken = default);
}