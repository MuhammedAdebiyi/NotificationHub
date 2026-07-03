using MediatR;
using NotificationHub.Application.Abstractions;
using NotificationHub.Shared.Abstractions;
using NotificationHub.Shared.Exceptions;
using NotificationHub.Domain.Entities; 

namespace NotificationHub.Application.Features.Notifications.Queries.GetNotificationById;

public class GetNotificationByIdQueryHandler
    : IRequestHandler<GetNotificationByIdQuery, Result<NotificationDto>>
{
    private readonly INotificationRepository _repository;
    private readonly ICurrentOrganization _currentOrganization;

    public GetNotificationByIdQueryHandler(
        INotificationRepository repository,
        ICurrentOrganization currentOrganization)
    {
        _repository = repository;
        _currentOrganization = currentOrganization;
    }

    public async Task<Result<NotificationDto>> Handle(
        GetNotificationByIdQuery request,
        CancellationToken cancellationToken)
    {
        if (_currentOrganization.OrganizationId is null)
            return Result<NotificationDto>.Failure("Organization context is missing.");

        var orgId = _currentOrganization.OrganizationId.Value;

        var notification = await _repository.GetByPublicIdAsync(
            orgId, request.PublicId, cancellationToken);

        if (notification is null)
            throw new NotFoundException(nameof(Notification), request.PublicId);

        return Result<NotificationDto>.Success(new NotificationDto(
            notification.PublicId,
            notification.RecipientEmail,
            notification.Type,
            notification.Channel.ToString(),
            notification.Status.ToString(),
            notification.Payload,
            notification.RetryCount,
            notification.CreatedAt
        ));
    }
}