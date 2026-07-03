using MediatR;
using NotificationHub.Application.Abstractions;
using NotificationHub.Shared.Abstractions;

namespace NotificationHub.Application.Features.Notifications.Queries.GetNotifications;

public class GetNotificationsQueryHandler
    : IRequestHandler<GetNotificationsQuery, Result<GetNotificationsResult>>
{
    private readonly INotificationRepository _repository;

    public GetNotificationsQueryHandler(INotificationRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<GetNotificationsResult>> Handle(
        GetNotificationsQuery request,
        CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _repository.GetPagedAsync(
            request.OrganizationId, request.Page, request.PageSize, cancellationToken);

        var dtos = items.Select(n => new NotificationListItemDto(
            n.PublicId,
            n.RecipientEmail,
            n.Type,
            n.Channel.ToString(),
            n.Status.ToString(),
            n.RetryCount,
            n.CreatedAt
        )).ToList();

        return Result<GetNotificationsResult>.Success(new GetNotificationsResult(
            dtos, totalCount, request.Page, request.PageSize));
    }
}