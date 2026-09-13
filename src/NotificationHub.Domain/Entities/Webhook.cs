using NotificationHub.Domain.Common;

namespace NotificationHub.Domain.Entities;

public class Webhook : AuditableEntity
{
    public Guid OrganizationId { get; set; }
    public string Url { get; set; } = string.Empty;
    public string? Secret { get; set; }
    public string[] Events { get; set; } = Array.Empty<string>();
    public bool IsActive { get; set; } = true;
    public DateTime? LastTriggeredAt { get; set; }
    public string? LastError { get; set; }

    public Organization? Organization { get; set; }
}
