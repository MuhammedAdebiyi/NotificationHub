using MediatR;
using Microsoft.AspNetCore.Mvc;
using NotificationHub.Application.Features.Notifications.Commands.CreateNotification;

namespace NotificationHub.Api.Controllers;

[ApiController]
[Route("api/notifications")]
public class NotificationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public NotificationsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateNotificationRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var command = new CreateNotificationCommand(
            request.UserId,
            request.Type,
            request.Channel,
            request.Payload,
            idempotencyKey
        );

        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
            return Conflict(new { error = result.Error });

        return Ok(new { publicId = result.Value });
    }
}

public record CreateNotificationRequest(
    Guid UserId,
    string Type,
    NotificationHub.Domain.Enums.NotificationChannel Channel,
    string Payload
);