using MediatR;
using NotificationHub.Application.Abstractions;
using NotificationHub.Shared.Abstractions;
using NotificationHub.Shared.Exceptions;

namespace NotificationHub.Application.Features.Notifications.Queries.GetNotificationById;

public class GetNotificationByIdQueryHandler
    : IRequestHandler<GetNotificationByIdQuery, Result<NotificationDto>>
{
    private readonly INotificationRepository _repository;

    public GetNotificationByIdQueryHandler(INotificationRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<NotificationDto>> Handle(
        GetNotificationByIdQuery request,
        CancellationToken cancellationToken)
    {
        var notification = await _repository.GetByPublicIdAsync(request.PublicId, cancellationToken);

        if (notification is null)
            throw new NotFoundException(nameof(notification), request.PublicId);

        var dto = new NotificationDto(
            notification.PublicId,
            notification.UserId,
            notification.Type,
            notification.Channel.ToString(),
            notification.Status.ToString(),
            notification.Payload,
            notification.RetryCount,
            notification.CreatedAt
        );

        return Result<NotificationDto>.Success(dto);
    }
}