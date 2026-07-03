using MediatR;
using NotificationHub.Application.Abstractions;
using NotificationHub.Domain.Entities;
using NotificationHub.Shared.Abstractions;

namespace NotificationHub.Application.Features.Notifications.Commands.CreateNotification;

public class CreateNotificationCommandHandler
    : IRequestHandler<CreateNotificationCommand, Result<Guid>>
{
    private readonly INotificationRepository _repository;

    public CreateNotificationCommandHandler(INotificationRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<Guid>> Handle(
        CreateNotificationCommand request,
        CancellationToken cancellationToken)
    {
        // Idempotency check
        if (!string.IsNullOrEmpty(request.IdempotencyKey))
        {
            var exists = await _repository.IdempotencyKeyExistsAsync(
                request.OrganizationId, request.IdempotencyKey, cancellationToken);

            if (exists)
                return Result<Guid>.Failure("Duplicate request: idempotency key already used.");
        }

        var notification = new Notification
        {
            OrganizationId = request.OrganizationId,
            RecipientEmail = request.RecipientEmail,
            Type = request.Type,
            Channel = request.Channel,
            Payload = request.Payload,
        };

        await _repository.AddAsync(notification, cancellationToken);

        if (!string.IsNullOrEmpty(request.IdempotencyKey))
        {
            await _repository.AddIdempotencyKeyAsync(new IdempotencyKey
            {
                OrganizationId = request.OrganizationId,
                Key = request.IdempotencyKey,
            }, cancellationToken);
        }

        await _repository.SaveChangesAsync(cancellationToken);

        // TODO: push to Redis queue (Phase 2 — worker wiring)
        // await _queue.EnqueueAsync(notification.Id, cancellationToken);

        return Result<Guid>.Success(notification.PublicId);
    }
}