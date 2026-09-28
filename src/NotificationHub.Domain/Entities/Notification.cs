using NotificationHub.Domain.Common;
using NotificationHub.Domain.Enums;

namespace NotificationHub.Domain.Entities;

public class Notification : BaseEntity
{
    // Public identifier exposed to clients
    public Guid PublicId { get; set; } = Guid.NewGuid();

    // Ownership
    public Guid OrganizationId { get; set; }
    public Guid? CampaignId { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public bool IsSuccess { get; set; } 

    // Recipient
    public string RecipientEmail { get; set; } = string.Empty;

    // Notification metadata
    public string Type { get; set; } = string.Empty;
    public NotificationChannel Channel { get; set; }
    public NotificationStatus Status { get; set; } = NotificationStatus.Pending;

    // Original payload
    public string Payload { get; set; } = string.Empty;

    // Retry information
    public int RetryCount { get; set; }

    // Worker lifecycle
    public string? WorkerId { get; private set; }

    /// <summary>
    /// When a worker successfully claimed this notification.
    /// </summary>
    public DateTime? AcceptedAt { get; private set; }

    /// <summary>
    /// When processing completed (success or permanent failure).
    /// </summary>
    public DateTime? ProcessedAt { get; private set; }

    /// <summary>
    /// When the provider confirmed delivery to the recipient's server
    /// (from a delivery webhook — not the same as ProcessedAt/accepted).
    /// </summary>
    public DateTime? DeliveredAt { get; private set; }

    // Provider information
    public string? Provider { get; private set; }

    /// <summary>
    /// Provider's message id (SendGrid id, SES id, etc.)
    /// </summary>
    public string? ProviderMessageId { get; private set; }

    // Failure information
    public string? LastError { get; private set; }

    // Read tracking
    public DateTime? OpenedAt { get; set; }

    public DateTime? NextRetryAt { get; private set; }

    // Navigation
    public Organization? Organization { get; set; }

    public Campaign? Campaign { get; set; }

    public ICollection<NotificationLog> Logs { get; set; } = new List<NotificationLog>();
   public void AssignWorker(string workerId)
    {
        WorkerId = workerId;
        AcceptedAt = DateTime.UtcNow;
        Status = NotificationStatus.Processing;
    }

    /// <summary>
    /// Provider's API accepted the message. This is NOT delivery confirmation —
    /// status stays Sent until a provider delivery webhook arrives.
    /// </summary>
    public void MarkAccepted(
        string provider,
        string providerMessageId)
    {
        Provider = provider;
        ProviderMessageId = providerMessageId;
        LastError = null;
        NextRetryAt = null;
        ProcessedAt = DateTime.UtcNow;
        Status = NotificationStatus.Sent;
    }

    /// <summary>
    /// Provider webhook confirmed final delivery. Idempotent: only a Sent
    /// notification transitions to Delivered; repeats are no-ops.
    /// </summary>
    public bool TryMarkProviderDelivered()
    {
        if (Status != NotificationStatus.Sent)
            return false;

        Status = NotificationStatus.Delivered;
        DeliveredAt = DateTime.UtcNow;
        LastError = null;
        return true;
    }

    /// <summary>
    /// Provider webhook reported a bounce or complaint. Idempotent.
    /// </summary>
    public bool TryMarkProviderBounced(string reason)
    {
        if (Status != NotificationStatus.Sent &&
            Status != NotificationStatus.Delivered)
            return false;

        Status = NotificationStatus.Bounced;
        LastError = reason;
        return true;
    }

    public void ScheduleRetry(
        DateTime nextRetryAt,
        string error)
    {
        RetryCount++;
        LastError = error;
        NextRetryAt = nextRetryAt;
        Status = NotificationStatus.Retrying;
    }

    public void MoveToDeadLetter(
        string reason)
    {
        LastError = reason;
        NextRetryAt = null;
        ProcessedAt = DateTime.UtcNow;
        Status = NotificationStatus.DeadLetter;
    }
    }