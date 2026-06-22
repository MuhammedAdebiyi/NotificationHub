using NotificationHub.Domain.Common;

namespace NotificationHub.Domain.Entities;

public class UserPreference : BaseEntity
{
    public Guid UserId { get; set; }
    public bool EmailEnabled { get; set; } = true;
    public bool SmsEnabled { get; set; } = true;
    public bool PushEnabled { get; set; } = true;

    public User? User { get; set; }
}