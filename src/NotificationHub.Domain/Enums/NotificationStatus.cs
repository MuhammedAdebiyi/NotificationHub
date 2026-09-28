namespace NotificationHub.Domain.Enums;

public enum NotificationStatus
{
    /// <summary>Saved to DB, not yet queued.</summary>
    Pending,

    /// <summary>Pushed onto the Redis queue, waiting for a worker slot.</summary>
    Queued,

    /// <summary>Worker has picked it up and is actively sending.</summary>
    Processing,

    /// <summary>Provider accepted the message successfully.</summary>
    Sent,

    /// <summary>Send attempt failed; scheduled for retry.</summary>
    Failed,

    /// <summary>Currently waiting for the next retry window.</summary>
    Retrying,

    /// <summary>Max retries exhausted; moved to dead-letter queue.</summary>
    DeadLetter,

    /// <summary>Provider confirmed final delivery to the recipient's mail server
    /// (set only from provider delivery webhooks, never optimistically).</summary>
    Delivered,

    /// <summary>Recipient's server rejected the message or the recipient
    /// complained (set only from provider webhooks).</summary>
    Bounced
}