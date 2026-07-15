namespace NotificationHub.Domain.Exceptions;

/// <summary>
/// Thrown when a notification cannot succeed no matter how many times it's retried
/// (malformed payload, invalid recipient, etc). The worker catches this specifically
/// and routes straight to DLQ instead of scheduling a backoff retry.
/// </summary>
public class NonRetriableNotificationException : Exception
{
    public NonRetriableNotificationException(string message) : base(message) { }
    public NonRetriableNotificationException(string message, Exception innerException)
        : base(message, innerException) { }
}