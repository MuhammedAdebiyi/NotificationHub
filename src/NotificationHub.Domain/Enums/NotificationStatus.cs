namespace NotificationHub.Domain.Enums;

public enum NotificationStatus
{
    Pending,
    Processing,
    Sent,
    Failed,
    Retrying,
    DeadLetter
}