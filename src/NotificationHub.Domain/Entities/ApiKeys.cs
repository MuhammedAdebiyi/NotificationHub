using NotificationHub.Domain.Common;

namespace NotificationHub.Domain.Entities;

public class ApiKey : AuditableEntity
{
    public Guid OrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string KeyHash { get; set; } = string.Empty;
    public string KeyPrefix { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime? LastUsedAt { get; set; }

    public Organization? Organization { get; set; }
}