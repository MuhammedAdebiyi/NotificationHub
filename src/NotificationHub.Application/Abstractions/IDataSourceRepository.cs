using NotificationHub.Domain.Entities;

namespace NotificationHub.Application.Abstractions;

public interface IDataSourceRepository
{
    Task<DataSource> AddAsync(DataSource dataSource, CancellationToken cancellationToken = default);

    Task<DataSource?> GetByIdAsync(
        Guid id, Guid organizationId, CancellationToken cancellationToken = default);

    Task<(List<DataSource> Items, int TotalCount)> GetPagedAsync(
        Guid organizationId, int page, int pageSize, CancellationToken cancellationToken = default);
}