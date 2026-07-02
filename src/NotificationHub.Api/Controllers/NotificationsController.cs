using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NotificationHub.Application.Features.Notifications.Commands.CreateNotification;
using NotificationHub.Application.Features.Notifications.Queries.GetNotifications;
using NotificationHub.Application.Features.Notifications.Queries.GetNotificationById;
namespace NotificationHub.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/notifications")]
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

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new GetNotificationsQuery(page, pageSize);
        var result = await _mediator.Send(query, cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return Ok(new
        {
            items = result.Value!.Items,
            totalCount = result.Value.TotalCount,
            pageNumber = result.Value.PageNumber,
            pageSize = result.Value.PageSize
        });
    }
    
    [HttpGet("{publicId:guid}")]
    public async Task<IActionResult> GetById(
        Guid publicId,
        CancellationToken cancellationToken)
    {
        var query = new GetNotificationByIdQuery(publicId);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result.Value);
    }
}

public record CreateNotificationRequest(
    Guid UserId,
    string Type,
    NotificationHub.Domain.Enums.NotificationChannel Channel,
    string Payload
);