using NotificationHub.Application.Features.Notifications.Queries.GetNotificationById;

namespace NotificationHub.Application.Abstractions;

public interface INotificationService
{
    Task<NotificationDetailDto?> GetDetailAsync(Guid organizationId, Guid publicId, CancellationToken ct = default);
    Task<List<NotificationLogDto>> GetLogsAsync(Guid organizationId, Guid publicId, CancellationToken ct = default);
    Task<bool> RetryAsync(Guid organizationId, Guid publicId, CancellationToken ct = default);

    /// <summary>
    /// Applies a provider delivery event (from the provider's inbound webhook)
    /// to the matching notification. Returns null when no notification matches
    /// the provider's message id (e.g. auth emails that never became rows).
    /// </summary>
    Task<ProviderEventOutcome?> ApplyProviderEventAsync(
        string emailId, string eventType, string? reason, CancellationToken ct = default);
}

/// <summary>Result of applying a provider delivery event.</summary>
public record ProviderEventOutcome(
    Guid PublicId,
    Guid OrganizationId,
    string Status,
    bool Changed);

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
    List<NotificationLogDto> Logs,

    DateTime? AcceptedAt,
    DateTime? ProcessedAt,
    string? WorkerId,
    string? ProviderMessageId,
    DateTime? DeliveredAt
);

public record NotificationLogDto(
    Guid Id,
    string Provider,
    string Response,
    bool IsSuccess,
    DateTime CreatedAt
);