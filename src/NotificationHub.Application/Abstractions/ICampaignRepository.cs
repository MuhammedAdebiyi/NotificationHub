using NotificationHub.Domain.Entities;
using NotificationHub.Domain.Enums;

namespace NotificationHub.Application.Abstractions;

public interface ICampaignRepository
{
    Task<Campaign?> GetByIdAsync(Guid id, Guid organizationId, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Campaign> Items, int TotalCount)> GetPagedAsync(Guid organizationId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Campaign>> GetRunningAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Campaign>> GetScheduledReadyAsync(DateTime now, CancellationToken cancellationToken = default);
    Task AddAsync(Campaign campaign, CancellationToken cancellationToken = default);
    Task AddRecipientsAsync(IEnumerable<CampaignRecipient> recipients, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CampaignRecipient>> GetUnprocessedRecipientsAsync(Guid campaignId, int batchSize, CancellationToken cancellationToken = default);
    Task<bool> RecipientExistsAsync(Guid campaignId, string email, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}