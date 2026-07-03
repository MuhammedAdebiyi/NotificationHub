using NotificationHub.Domain.Entities;

namespace NotificationHub.Application.Abstractions;

public interface INotificationRepository
{
    Task AddAsync(Notification notification, CancellationToken cancellationToken = default);
    Task<Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Notification?> GetByPublicIdAsync(Guid organizationId, Guid publicId, CancellationToken cancellationToken = default);
    Task<bool> IdempotencyKeyExistsAsync(Guid organizationId, string key, CancellationToken cancellationToken = default);
    Task AddIdempotencyKeyAsync(IdempotencyKey idempotencyKey, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Notification> Items, int TotalCount)> GetPagedAsync(Guid organizationId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}