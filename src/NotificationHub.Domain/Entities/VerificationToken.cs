using NotificationHub.Domain.Common;

namespace NotificationHub.Domain.Entities;

public class VerificationToken : BaseEntity
{
    public Guid UserId { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public bool IsUsed { get; set; } = false;

    public User? User { get; set; }
}