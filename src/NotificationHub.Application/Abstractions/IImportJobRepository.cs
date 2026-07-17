using NotificationHub.Domain.Entities;

namespace NotificationHub.Application.Abstractions;

public interface IImportJobRepository
{
    Task<ImportJob> AddAsync(ImportJob job, CancellationToken cancellationToken = default);
    Task<ImportJob?> GetByIdAsync(Guid id, Guid organizationId, CancellationToken cancellationToken = default);
    Task<List<ImportJob>> GetPendingAsync(CancellationToken cancellationToken = default);
    Task<List<ImportJob>> GetRunningAsync(CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}