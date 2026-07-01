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

    public CreateNotificationCommandHandler(
        INotificationRepository repository,
        INotificationQueue queue)
    {
        _repository = repository;
        _queue = queue;
    }

    public async Task<Result<Guid>> Handle(
        CreateNotificationCommand request,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            var exists = await _repository.IdempotencyKeyExistsAsync(
                request.IdempotencyKey, cancellationToken);

            if (exists)
                return Result<Guid>.Failure("Duplicate request: idempotency key already used.");

            await _repository.AddIdempotencyKeyAsync(
                new IdempotencyKey { Key = request.IdempotencyKey },
                cancellationToken);
        }

        var notification = new Notification
        {
            UserId  = request.UserId,
            Type    = request.Type,
            Channel = request.Channel,
            Payload = request.Payload,
        };

        await _repository.AddAsync(notification, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        // Persist succeeded — now enqueue for async processing
        await _queue.EnqueueAsync(notification.Id, cancellationToken);

        return Result<Guid>.Success(notification.PublicId);
    }
}