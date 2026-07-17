using NotificationHub.Domain.Entities;
using NotificationHub.Domain.Enums;

namespace NotificationHub.Application.Abstractions;

public record CreateDataSourceCommand(
    Guid OrganizationId,
    Guid UserId,
    string Name,
    DataSourceType Type,
    string ConnectionString,
    string? Host,
    string? Database);

public interface IDataSourceService
{
    Task<DataSource> CreateAsync(CreateDataSourceCommand command, CancellationToken cancellationToken = default);
    Task<List<string>> GetTablesAsync(Guid dataSourceId, Guid organizationId, CancellationToken cancellationToken = default);
    Task<List<ColumnInfo>> GetColumnsAsync(Guid dataSourceId, Guid organizationId, string tableName, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid dataSourceId, Guid organizationId, CancellationToken cancellationToken = default);
}