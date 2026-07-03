using MediatR;
using NotificationHub.Application.Abstractions;
using NotificationHub.Shared.Abstractions;

namespace NotificationHub.Application.Features.Notifications.Queries.GetNotifications;

public class GetNotificationsQueryHandler
    : IRequestHandler<GetNotificationsQuery, Result<GetNotificationsResult>>
{
    private readonly INotificationRepository _repository;
    private readonly ICurrentOrganization _currentOrganization;

    public GetNotificationsQueryHandler(
        INotificationRepository repository,
        ICurrentOrganization currentOrganization)
    {
        _repository = repository;
        _currentOrganization = currentOrganization;
    }

    public async Task<Result<GetNotificationsResult>> Handle(
        GetNotificationsQuery request,
        CancellationToken cancellationToken)
    {
        if (_currentOrganization.OrganizationId is null)
            return Result<GetNotificationsResult>.Failure("Organization context is missing.");

        var orgId = _currentOrganization.OrganizationId.Value;

        var (items, totalCount) = await _repository.GetPagedAsync(
            orgId, request.Page, request.PageSize, cancellationToken);

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
            dtos,
            totalCount,
            request.Page,
            request.PageSize
        ));
    }
}