using MediatR;
using NotificationHub.Domain.Enums;
using NotificationHub.Shared.Abstractions;

namespace NotificationHub.Application.Features.Notifications.Commands.CreateNotification;

public record CreateNotificationCommand(
    string RecipientEmail,
    string Type,
    NotificationChannel Channel,
    string Payload,
    string? IdempotencyKey = null
) : IRequest<Result<Guid>>;