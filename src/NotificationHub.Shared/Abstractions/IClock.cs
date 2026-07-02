using NotificationHub.Shared.Abstractions;
namespace NotificationHub.Shared.Abstractions;

public interface IClock
{
    DateTime UtcNow { get; }
}