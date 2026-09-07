using NotificationHub.Domain.Common;

namespace NotificationHub.Domain.Entities;

public class EmailProviderConfig : AuditableEntity
{
    public Guid OrganizationId { get; set; }
    public string ProviderType { get; set; } = string.Empty; // "resend"
    public string EncryptedApiKey { get; set; } = string.Empty;
    public string? SenderEmail { get; set; }
    public bool IsActive { get; set; } = true;

    public Organization? Organization { get; set; }
}
