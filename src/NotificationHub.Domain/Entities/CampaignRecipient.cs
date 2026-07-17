using NotificationHub.Domain.Common;

namespace NotificationHub.Domain.Entities;

public class CampaignRecipient : BaseEntity
{
    public Guid CampaignId { get; set; }
    public Guid OrganizationId { get; set; }
    public string RecipientEmail { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public Guid? NotificationId { get; set; }

    public Campaign? Campaign { get; set; }
    public Notification? Notification { get; set; }
}