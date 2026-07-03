using NotificationHub.Domain.Common;

namespace NotificationHub.Domain.Entities;

public class IdempotencyKey : BaseEntity
{
    public Guid OrganizationId { get; set; }
    public string Key { get; set; } = string.Empty;
}