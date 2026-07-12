using NotificationHub.Application.Features.Notifications.Queries.GetNotificationById;

namespace NotificationHub.Application.Abstractions;

public interface INotificationService
{
    Task<NotificationDetailDto?> GetDetailAsync(Guid organizationId, Guid publicId, CancellationToken ct = default);
    Task<List<NotificationLogDto>> GetLogsAsync(Guid organizationId, Guid publicId, CancellationToken ct = default);
    Task<bool> RetryAsync(Guid organizationId, Guid publicId, CancellationToken ct = default);
}

public record NotificationDetailDto(
    Guid PublicId,
    string RecipientEmail,
    string Type,
    string Channel,
    string Status,
    string Payload,
    int RetryCount,
    DateTime CreatedAt,
    string? LastProvider,
    string? LastError,
    List<NotificationLogDto> Logs
);

public record NotificationLogDto(
    Guid Id,
    string Provider,
    string Response,
    bool IsSuccess,
    DateTime CreatedAt
);