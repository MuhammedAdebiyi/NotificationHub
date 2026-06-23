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

    public async Task<Notification?> GetByPublicIdAsync(Guid publicId, CancellationToken cancellationToken = default)
        => await _context.Notifications
            .FirstOrDefaultAsync(n => n.PublicId == publicId, cancellationToken);

    public async Task<bool> IdempotencyKeyExistsAsync(string key, CancellationToken cancellationToken = default)
        => await _context.IdempotencyKeys
            .AnyAsync(k => k.Key == key, cancellationToken);

    public async Task AddIdempotencyKeyAsync(IdempotencyKey key, CancellationToken cancellationToken = default)
        => await _context.IdempotencyKeys.AddAsync(key, cancellationToken);

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => await _context.SaveChangesAsync(cancellationToken);
}