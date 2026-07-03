using Microsoft.EntityFrameworkCore;
using NotificationHub.Application.Abstractions;
using NotificationHub.Domain.Entities;
using NotificationHub.Infrastructure.Persistence;

namespace NotificationHub.Infrastructure.Repositories;

public class NotificationRepository : INotificationRepository
{
    private readonly AppDbContext _context;

    public NotificationRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Notification notification, CancellationToken cancellationToken = default)
        => await _context.Notifications.AddAsync(notification, cancellationToken);

    public async Task<Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _context.Notifications.FindAsync([id], cancellationToken);

    public async Task<Notification?> GetByPublicIdAsync(Guid publicId, Guid organizationId, CancellationToken cancellationToken = default)
        => await _context.Notifications
            .FirstOrDefaultAsync(n => n.PublicId == publicId && n.OrganizationId == organizationId, cancellationToken);

    public async Task<bool> IdempotencyKeyExistsAsync(string key, Guid organizationId, CancellationToken cancellationToken = default)
        => await _context.IdempotencyKeys
            .AnyAsync(k => k.Key == key && k.OrganizationId == organizationId, cancellationToken);

    public async Task AddIdempotencyKeyAsync(IdempotencyKey idempotencyKey, CancellationToken cancellationToken = default)
        => await _context.IdempotencyKeys.AddAsync(idempotencyKey, cancellationToken);

    public async Task<(IReadOnlyList<Notification> Items, int TotalCount)> GetPagedAsync(
        Guid organizationId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _context.Notifications
            .Where(n => n.OrganizationId == organizationId)
            .OrderByDescending(n => n.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => await _context.SaveChangesAsync(cancellationToken);
}