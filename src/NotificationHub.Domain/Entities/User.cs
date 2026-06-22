using NotificationHub.Domain.Common;

namespace NotificationHub.Domain.Entities;

public class User : AuditableEntity
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public bool IsEmailVerified { get; set; }

    public UserPreference? Preferences { get; set; }
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}