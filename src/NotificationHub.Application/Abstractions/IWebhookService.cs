namespace NotificationHub.Application.Abstractions;

public interface IWebhookService
{
    Task DispatchAsync(string eventName, Guid organizationId, object payload);
}
