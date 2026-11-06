using MediatR;
using NotificationHub.Domain.Enums;
using NotificationHub.Shared.Abstractions;

namespace NotificationHub.Application.Features.Notifications.Commands.CreateNotification;

// Optional addressing fields are resolved once at enqueue time — see
// AddressResolution for the precedence order.
public record CreateNotificationCommand(
    Guid OrganizationId,
    Guid UserId,
    string RecipientEmail,
    string Type,
    NotificationChannel Channel,
    string Payload,
    string? IdempotencyKey,
    // Single reply-to address supplied by the caller (highest precedence).
    string? ReplyTo = null,
    // Sender address/display name supplied by the caller.
    string? SenderEmail = null,
    string? SenderName = null,
    // Template whose reply-to/sender apply when the request sets none of its own.
    Guid? TemplateId = null
) : IRequest<Result<Guid>>;
