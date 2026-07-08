using Microsoft.EntityFrameworkCore;
using NotificationHub.Application.Abstractions;
using NotificationHub.Domain.Entities;
using NotificationHub.Domain.Enums;
using NotificationHub.Infrastructure.Persistence;

namespace NotificationHub.Infrastructure.Repositories;

public class CampaignRepository : ICampaignRepository
{
    private readonly AppDbContext _context;

    public CampaignRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Campaign?> GetByIdAsync(Guid id, Guid organizationId, CancellationToken cancellationToken = default)
        => await _context.Campaigns
            .Include(c => c.Recipients)
            .FirstOrDefaultAsync(c => c.Id == id && c.OrganizationId == organizationId && c.DeletedAt == null, cancellationToken);

    public async Task<(IReadOnlyList<Campaign> Items, int TotalCount)> GetPagedAsync(
        Guid organizationId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _context.Campaigns
            .Where(c => c.OrganizationId == organizationId && c.DeletedAt == null)
            .OrderByDescending(c => c.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<IReadOnlyList<Campaign>> GetRunningAsync(CancellationToken cancellationToken = default)
        => await _context.Campaigns
            .Where(c => c.Status == CampaignStatus.Running && c.DeletedAt == null)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Campaign>> GetScheduledReadyAsync(DateTime now, CancellationToken cancellationToken = default)
        => await _context.Campaigns
            .Where(c => c.Status == CampaignStatus.Scheduled && c.ScheduledAt <= now && c.DeletedAt == null)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(Campaign campaign, CancellationToken cancellationToken = default)
        => await _context.Campaigns.AddAsync(campaign, cancellationToken);

    public async Task AddRecipientsAsync(IEnumerable<CampaignRecipient> recipients, CancellationToken cancellationToken = default)
        => await _context.CampaignRecipients.AddRangeAsync(recipients, cancellationToken);

    public async Task<IReadOnlyList<CampaignRecipient>> GetUnprocessedRecipientsAsync(
        Guid campaignId, int batchSize, CancellationToken cancellationToken = default)
        => await _context.CampaignRecipients
            .Where(r => r.CampaignId == campaignId && r.NotificationId == null)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

    public async Task<bool> RecipientExistsAsync(Guid campaignId, string email, CancellationToken cancellationToken = default)
        => await _context.CampaignRecipients
            .AnyAsync(r => r.CampaignId == campaignId && r.RecipientEmail == email, cancellationToken);

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => await _context.SaveChangesAsync(cancellationToken);
}