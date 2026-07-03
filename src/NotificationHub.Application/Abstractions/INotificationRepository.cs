using NotificationHub.Domain.Entities;

namespace NotificationHub.Application.Abstractions;

public interface INotificationRepository
{
    Task AddAsync(Notification notification, CancellationToken cancellationToken = default);
    Task<Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Notification?> GetByPublicIdAsync(Guid publicId, Guid organizationId, CancellationToken cancellationToken = default);
    Task<bool> IdempotencyKeyExistsAsync(string key, Guid organizationId, CancellationToken cancellationToken = default);
    Task AddIdempotencyKeyAsync(IdempotencyKey idempotencyKey, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Notification> Items, int TotalCount)> GetPagedAsync(
        Guid organizationId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}