using NotificationHub.Domain.Common;

namespace NotificationHub.Domain.Entities;

public class Template : AuditableEntity
{
    public Guid OrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public Guid? CreatedByUserId { get; set; }
    public string Body { get; set; } = string.Empty;

    // Optional addressing defaults for notifications created from this
    // template. They sit below the per-request values in the resolution
    // precedence and above the organization settings.
    public string? ReplyTo { get; set; }
    public string? SenderEmail { get; set; }
    public string? SenderName { get; set; }

    public Organization? Organization { get; set; }
}