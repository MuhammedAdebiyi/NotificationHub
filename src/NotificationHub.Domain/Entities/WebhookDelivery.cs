using NotificationHub.Domain.Common;

namespace NotificationHub.Domain.Entities;

public class WebhookDelivery : AuditableEntity
{
    public Guid WebhookId { get; set; }
    public string Event { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public string? Response { get; set; }
    public bool IsSuccess { get; set; }
    public double DurationMs { get; set; }

    public Webhook? Webhook { get; set; }
}
