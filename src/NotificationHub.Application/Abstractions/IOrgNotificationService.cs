namespace NotificationHub.Application.Abstractions;

public interface IOrgNotificationService
{
    Task NotifyOrgAsync(
        Guid organizationId,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken = default);
}