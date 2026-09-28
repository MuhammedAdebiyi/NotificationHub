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
            SensitiveDataMasker.Mask(l.Response),
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
            SensitiveDataMasker.Mask(notification.Payload),
            notification.RetryCount,
            notification.CreatedAt,
            lastLog?.Provider,
            SensitiveDataMasker.Mask(lastLog?.Response),
            logs,
            notification.AcceptedAt,
            notification.ProcessedAt,
            notification.WorkerId,
            notification.ProviderMessageId,
            notification.DeliveredAt
        );
    }

    public async Task<List<NotificationLogDto>> GetLogsAsync(
        Guid organizationId, Guid publicId, CancellationToken ct = default)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n =>
                n.OrganizationId == organizationId && n.PublicId == publicId, ct);

        if (notification is null) return new List<NotificationLogDto>();

        var rows = await _context.NotificationLogs
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

        return rows
            .Select(l => l with { Response = SensitiveDataMasker.Mask(l.Response) })
            .ToList();
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

    public async Task<ProviderEventOutcome?> ApplyProviderEventAsync(
        string emailId, string eventType, string? reason, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(emailId))
            return null;

        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.ProviderMessageId == emailId, ct);

        // Older sends stored an empty provider message id — the delivery log
        // still carries "accepted: id=em_..." so fall back to matching on it.
        if (notification is null)
        {
            var logMarker = $"accepted: id={emailId}";
            notification = await _context.Notifications
                .Where(n => n.Logs.Any(l => l.Response == logMarker))
                .OrderByDescending(n => n.CreatedAt)
                .FirstOrDefaultAsync(ct);
        }

        if (notification is null)
            return null;

        var changed = eventType switch
        {
            "email.delivered" => notification.TryMarkProviderDelivered(),
            "email.bounced" => notification.TryMarkProviderBounced(
                string.IsNullOrWhiteSpace(reason) ? "Bounced by recipient server" : reason),
            "email.complained" => notification.TryMarkProviderBounced(
                "Recipient marked the email as spam"),
            _ => false
        };

        if (changed)
            await _context.SaveChangesAsync(ct);

        return new ProviderEventOutcome(
            notification.PublicId,
            notification.OrganizationId,
            notification.Status.ToString(),
            changed);
    }
}