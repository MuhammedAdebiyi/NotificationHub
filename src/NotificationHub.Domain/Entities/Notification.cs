using NotificationHub.Domain.Common;
using NotificationHub.Domain.Enums;

namespace NotificationHub.Domain.Entities;

public class Notification : BaseEntity
{
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid? CampaignId { get; set; }
    public string Type { get; set; } = string.Empty;
    public NotificationChannel Channel { get; set; }
    public NotificationStatus Status { get; set; } = NotificationStatus.Pending;
    public string Payload { get; set; } = string.Empty;
    public int RetryCount { get; set; } = 0;

    public User? User { get; set; }
    public Campaign? Campaign { get; set; }
    public ICollection<NotificationLog> Logs { get; set; } = new List<NotificationLog>();
}