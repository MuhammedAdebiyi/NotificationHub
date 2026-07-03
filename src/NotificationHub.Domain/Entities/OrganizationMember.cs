using NotificationHub.Domain.Common;

namespace NotificationHub.Domain.Entities;

public class OrganizationMember : BaseEntity
{
    public Guid OrganizationId { get; set; }
    public Guid UserId { get; set; }
    public string Role { get; set; } = "member"; // owner | admin | member
    public DateTime? InvitedAt { get; set; }
    public DateTime? JoinedAt { get; set; }

    public Organization? Organization { get; set; }
    public User? User { get; set; }
}