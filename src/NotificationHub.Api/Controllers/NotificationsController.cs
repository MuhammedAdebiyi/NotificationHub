using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NotificationHub.Application.Abstractions;
using NotificationHub.Application.Features.Notifications.Commands.CreateNotification;
using NotificationHub.Application.Features.Notifications.Queries.GetNotificationById;
using NotificationHub.Application.Features.Notifications.Queries.GetNotifications;
using NotificationHub.Domain.Enums;
using NotificationHub.Infrastructure.Persistence;
using NotificationHub.Shared.Abstractions;

namespace NotificationHub.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/notifications")]
public class NotificationsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentOrganization _currentOrg;
    private readonly INotificationRepository _notificationRepository;
    private readonly INotificationQueue _queue;
    private readonly AppDbContext _context;

    public NotificationsController(
        IMediator mediator,
        ICurrentOrganization currentOrg,
        INotificationRepository notificationRepository,
        INotificationQueue queue,
        AppDbContext context)
    {
        _mediator = mediator;
        _currentOrg = currentOrg;
        _notificationRepository = notificationRepository;
        _queue = queue;
        _context = context;
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

    [HttpGet("{publicId:guid}/logs")]
    public async Task<IActionResult> GetLogs(
        Guid publicId,
        CancellationToken cancellationToken)
    {
        if (!_currentOrg.IsAuthenticated || _currentOrg.OrganizationId is null)
            return Unauthorized(new { error = "No organization context." });

        var notification = await _notificationRepository.GetByPublicIdAsync(
            _currentOrg.OrganizationId.Value, publicId, cancellationToken);

        if (notification is null)
            return NotFound(new { error = "Notification not found." });

        var logs = await _context.NotificationLogs
            .Where(l => l.NotificationId == notification.Id)
            .OrderBy(l => l.CreatedAt)
            .Select(l => new
            {
                l.Id,
                l.Provider,
                l.Response,
                l.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return Ok(logs);
    }

    [HttpPost("{publicId:guid}/retry")]
    public async Task<IActionResult> Retry(
        Guid publicId,
        CancellationToken cancellationToken)
    {
        if (!_currentOrg.IsAuthenticated || _currentOrg.OrganizationId is null)
            return Unauthorized(new { error = "No organization context." });

        var notification = await _notificationRepository.GetByPublicIdAsync(
            _currentOrg.OrganizationId.Value, publicId, cancellationToken);

        if (notification is null)
            return NotFound(new { error = "Notification not found." });

        if (notification.Status != NotificationStatus.DeadLetter &&
            notification.Status != NotificationStatus.Failed)
            return BadRequest(new { error = "Only Failed or DeadLetter notifications can be retried." });

        notification.Status = NotificationStatus.Pending;
        notification.RetryCount = 0;
        await _notificationRepository.SaveChangesAsync(cancellationToken);
        await _queue.EnqueueAsync(notification.Id, cancellationToken);

        return Ok(new { queued = true, publicId });
    }
}

public record CreateNotificationRequest(
    string RecipientEmail,
    string Type,
    NotificationHub.Domain.Enums.NotificationChannel Channel,
    string Payload
);