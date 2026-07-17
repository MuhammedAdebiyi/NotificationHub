using NotificationHub.Domain.Entities;
using NotificationHub.Domain.Enums;

namespace NotificationHub.Application.Abstractions;

public record CreateCampaignDto(
    Guid OrganizationId,
    Guid? CreatedByUserId,
    string Title,
    string Subject,
    string? Body,
    Guid? TemplateId,
    NotificationChannel Channel,
    DateTime? ScheduledAt
);

public record AddRecipientsDto(
    Guid CampaignId,
    Guid OrganizationId,
    List<string> RawEmails
);

public record ImportedRecipient(string Email, string? FirstName, string? LastName);

public record AddRecipientsWithNamesDto(
    Guid CampaignId,
    Guid OrganizationId,
    List<ImportedRecipient> Recipients
);

public record AddRecipientsResult(int Added, int Skipped);

public interface ICampaignService
{
    Task<Campaign> CreateAsync(CreateCampaignDto dto, CancellationToken cancellationToken = default);
    Task<AddRecipientsResult> AddRecipientsAsync(AddRecipientsDto dto, CancellationToken cancellationToken = default);
    Task<AddRecipientsResult> AddRecipientsWithNamesAsync(AddRecipientsWithNamesDto dto, CancellationToken cancellationToken = default);
    Task SendAsync(Guid campaignId, Guid organizationId, Guid? triggeredByUserId, CancellationToken cancellationToken = default);
    Task ScheduleAsync(Guid campaignId, Guid organizationId, DateTime scheduledAt, CancellationToken cancellationToken = default);
    Task PauseAsync(Guid campaignId, Guid organizationId, CancellationToken cancellationToken = default);
    Task ResumeAsync(Guid campaignId, Guid organizationId, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid campaignId, Guid organizationId, CancellationToken cancellationToken = default);
}