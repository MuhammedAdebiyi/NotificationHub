using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NotificationHub.Application.Features.Notifications.Commands.CreateNotification;
using NotificationHub.Application.Features.Notifications.Queries.GetNotificationById;
using NotificationHub.Application.Features.Notifications.Queries.GetNotifications;
using NotificationHub.Shared.Abstractions;

namespace NotificationHub.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/notifications")]
public class NotificationsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentOrganization _currentOrg;

    public NotificationsController(IMediator mediator, ICurrentOrganization currentOrg)
    {
        _mediator = mediator;
        _currentOrg = currentOrg;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateNotificationRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        if (!_currentOrg.IsAuthenticated || _currentOrg.OrganizationId is null)
            return Unauthorized(new { error = "No organization context. Use JWT or X-Api-Key." });

        var command = new CreateNotificationCommand(
            _currentOrg.OrganizationId.Value,
            request.RecipientEmail,
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
        if (!_currentOrg.IsAuthenticated || _currentOrg.OrganizationId is null)
            return Unauthorized(new { error = "No organization context." });

        var query = new GetNotificationsQuery(
            _currentOrg.OrganizationId.Value, page, pageSize);

        var result = await _mediator.Send(query, cancellationToken);

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
        if (!_currentOrg.IsAuthenticated || _currentOrg.OrganizationId is null)
            return Unauthorized(new { error = "No organization context." });

        var query = new GetNotificationByIdQuery(
            _currentOrg.OrganizationId.Value, publicId);

        var result = await _mediator.Send(query, cancellationToken);

        if (!result.IsSuccess)
            return NotFound(new { error = result.Error });

        return Ok(result.Value);
    }
}

public record CreateNotificationRequest(
    string RecipientEmail,
    string Type,
    NotificationHub.Domain.Enums.NotificationChannel Channel,
    string Payload
);