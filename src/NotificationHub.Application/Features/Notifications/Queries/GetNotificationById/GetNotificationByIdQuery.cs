using MediatR;
using NotificationHub.Shared.Abstractions;

namespace NotificationHub.Application.Features.Notifications.Queries.GetNotificationById;

public record GetNotificationByIdQuery(Guid PublicId) : IRequest<Result<NotificationDto>>;

public record NotificationDto(
    Guid PublicId,
    Guid UserId,
    string Type,
    string Channel,
    string Status,
    string Payload,
    int RetryCount,
    DateTime CreatedAt
);