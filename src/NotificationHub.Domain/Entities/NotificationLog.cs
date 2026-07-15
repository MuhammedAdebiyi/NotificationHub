using NotificationHub.Domain.Common;

namespace NotificationHub.Domain.Entities;

public class NotificationLog : BaseEntity
{
    public Guid NotificationId { get; set; }
    public Guid OrganizationId { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string Response { get; set; } = string.Empty;
    public bool IsSuccess { get; set; }

    public Notification? Notification { get; set; }
}