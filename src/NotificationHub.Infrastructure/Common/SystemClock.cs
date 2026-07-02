using NotificationHub.Shared.Abstractions;

namespace NotificationHub.Infrastructure.Common;

public class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}