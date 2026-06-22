using NotificationHub.Domain.Common;

namespace NotificationHub.Domain.Entities;

public class Template : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
}