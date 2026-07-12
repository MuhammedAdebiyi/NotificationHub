using NotificationHub.Domain.Common;

namespace NotificationHub.Domain.Entities;

public class OrgInvite : BaseEntity
{
    public Guid OrganizationId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string Role { get; set; } = "member";
    public DateTime ExpiresAt { get; set; }
    public bool IsAccepted { get; set; } = false;
    public DateTime InvitedAt { get; set; } = DateTime.UtcNow;

    public Organization? Organization { get; set; }
}