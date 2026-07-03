using MediatR;
using NotificationHub.Shared.Abstractions;

namespace NotificationHub.Application.Features.Notifications.Queries.GetNotifications;

public record GetNotificationsQuery(Guid? UserId, int Page = 1, int PageSize = 20)
    : IRequest<Result<GetNotificationsResult>>;

public record GetNotificationsResult(
    IReadOnlyList<NotificationListItemDto> Items,
    int TotalCount,
    int PageNumber,
    int PageSize
);

public record NotificationListItemDto(
    Guid PublicId,
    Guid UserId,
    string Type,
    string Channel,
    string Status,
    int RetryCount,
    DateTime CreatedAt
);