namespace NotificationHub.Application.Abstractions;

public interface IOrgNotificationService
{
    Task NotifyAsync(
        Guid organizationId,
        string subject,
        string html,
        string text,
        CancellationToken cancellationToken = default);
}