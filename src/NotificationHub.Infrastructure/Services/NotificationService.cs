using Microsoft.EntityFrameworkCore;
using NotificationHub.Application.Abstractions;
using NotificationHub.Domain.Enums;
using NotificationHub.Infrastructure.Persistence;

namespace NotificationHub.Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly AppDbContext _context;
    private readonly INotificationQueue _queue;

    public NotificationService(AppDbContext context, INotificationQueue queue)
    {
        _context = context;
        _queue = queue;
    }

    public async Task<NotificationDetailDto?> GetDetailAsync(
        Guid organizationId, Guid publicId, CancellationToken ct = default)
    {
        var notification = await _context.Notifications
            .Include(n => n.Logs.OrderBy(l => l.CreatedAt))
            .FirstOrDefaultAsync(n =>
                n.OrganizationId == organizationId && n.PublicId == publicId, ct);

        if (notification is null) return null;

        var logs = notification.Logs.Select(l => new NotificationLogDto(
            l.Id,
            l.Provider,
            l.Response,
            !l.Response.Contains("error", StringComparison.OrdinalIgnoreCase) &&
            !l.Response.Contains("fail", StringComparison.OrdinalIgnoreCase),
            l.CreatedAt
        )).ToList();

        var lastLog = notification.Logs.OrderByDescending(l => l.CreatedAt).FirstOrDefault();

        return new NotificationDetailDto(
            notification.PublicId,
            notification.RecipientEmail,
            notification.Type,
            notification.Channel.ToString(),
            notification.Status.ToString(),
            notification.Payload,
            notification.RetryCount,
            notification.CreatedAt,
            lastLog?.Provider,
            lastLog?.Response,
            logs
        );
    }

    public async Task<List<NotificationLogDto>> GetLogsAsync(
        Guid organizationId, Guid publicId, CancellationToken ct = default)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n =>
                n.OrganizationId == organizationId && n.PublicId == publicId, ct);

        if (notification is null) return new List<NotificationLogDto>();

        return await _context.NotificationLogs
            .Where(l => l.NotificationId == notification.Id)
            .OrderBy(l => l.CreatedAt)
            .Select(l => new NotificationLogDto(
                l.Id,
                l.Provider,
                l.Response,
                !l.Response.Contains("error") && !l.Response.Contains("fail"),
                l.CreatedAt
            ))
            .ToListAsync(ct);
    }

    public async Task<bool> RetryAsync(
        Guid organizationId, Guid publicId, CancellationToken ct = default)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n =>
                n.OrganizationId == organizationId && n.PublicId == publicId, ct);

        if (notification is null) return false;

        if (notification.Status != NotificationStatus.Failed &&
            notification.Status != NotificationStatus.DeadLetter)
            return false;

        notification.Status = NotificationStatus.Pending;
        notification.RetryCount = 0;
        await _context.SaveChangesAsync(ct);
        await _queue.EnqueueAsync(notification.Id, ct);

        return true;
    }
}