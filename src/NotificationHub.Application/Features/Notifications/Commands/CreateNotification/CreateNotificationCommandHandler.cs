using MediatR;
using NotificationHub.Application.Abstractions;
using NotificationHub.Domain.Entities;
using NotificationHub.Shared.Abstractions;

namespace NotificationHub.Application.Features.Notifications.Commands.CreateNotification;

public class CreateNotificationCommandHandler
    : IRequestHandler<CreateNotificationCommand, Result<Guid>>
{
    private readonly INotificationRepository _repository;
    private readonly INotificationQueue _queue;
    private readonly ICurrentOrganization _currentOrganization;

    public CreateNotificationCommandHandler(
        INotificationRepository repository,
        INotificationQueue queue,
        ICurrentOrganization currentOrganization)
    {
        _repository = repository;
        _queue = queue;
        _currentOrganization = currentOrganization;
    }

    public async Task<Result<Guid>> Handle(
        CreateNotificationCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentOrganization.OrganizationId is null)
            return Result<Guid>.Failure("Organization context is missing.");

        var orgId = _currentOrganization.OrganizationId.Value;

        if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            var exists = await _repository.IdempotencyKeyExistsAsync(
                request.IdempotencyKey, orgId, cancellationToken);

            if (exists)
                return Result<Guid>.Failure("Duplicate request: idempotency key already used.");

            await _repository.AddIdempotencyKeyAsync(
                new IdempotencyKey { OrganizationId = orgId, Key = request.IdempotencyKey },
                cancellationToken);
        }

        var notification = new Notification
        {
            OrganizationId = orgId,
            RecipientEmail = request.RecipientEmail,
            Type           = request.Type,
            Channel        = request.Channel,
            Payload        = request.Payload,
        };

        await _repository.AddAsync(notification, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        await _queue.EnqueueAsync(notification.Id, cancellationToken);

        return Result<Guid>.Success(notification.PublicId);
    }
}