using MediatR;
using NotificationHub.Domain.Enums;
using NotificationHub.Shared.Abstractions;

namespace NotificationHub.Application.Features.Notifications.Commands.CreateNotification;

public record CreateNotificationCommand(
    Guid OrganizationId,
    string RecipientEmail,
    string Type,
    NotificationChannel Channel,
    string Payload,
    string? IdempotencyKey
) : IRequest<Result<Guid>>;