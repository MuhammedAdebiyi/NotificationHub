using NotificationHub.Domain.Common;

namespace NotificationHub.Domain.Entities;

public class CampaignRecipient : BaseEntity
{
    public Guid CampaignId { get; set; }
    public Guid UserId { get; set; }
    public Guid? NotificationId { get; set; }

    public Campaign? Campaign { get; set; }
    public User? User { get; set; }
    public Notification? Notification { get; set; }
}