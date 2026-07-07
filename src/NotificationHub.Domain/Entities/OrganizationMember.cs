using NotificationHub.Domain.Common;

namespace NotificationHub.Domain.Entities;

public class OrganizationMember : BaseEntity
{
    public Guid OrganizationId { get; set; }
    public Guid UserId { get; set; }
    public string Role { get; set; } = "member"; // owner | admin | member | revoked
    public DateTime? InvitedAt { get; set; }
    public DateTime? JoinedAt { get; set; }
    public DateTime? RevokedAt { get; set; }

    public Organization? Organization { get; set; }
    public User? User { get; set; }

    public bool IsRevoked => Role == "revoked";
}