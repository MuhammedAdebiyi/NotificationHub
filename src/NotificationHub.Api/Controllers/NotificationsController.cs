using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NotificationHub.Application.Abstractions;
using NotificationHub.Application.Features.Notifications.Commands.CreateNotification;
using NotificationHub.Application.Features.Notifications.Queries.GetNotifications;
using NotificationHub.Shared.Abstractions;

namespace NotificationHub.Api.Controllers;

/// <summary>
/// Send and manage email notifications.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/notifications")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentOrganization _currentOrg;
    private readonly ICurrentUser _currentUser;
    private readonly INotificationService _notificationService;

    public NotificationsController(
        IMediator mediator,
        ICurrentOrganization currentOrg,
        ICurrentUser currentUser,
        INotificationService notificationService)
    {
        _mediator = mediator;
        _currentOrg = currentOrg;
        _currentUser = currentUser;
        _notificationService = notificationService;
    }

    /// <summary>
    /// Send a new notification (email, SMS, push, or in-app).
    /// </summary>
    /// <remarks>
    /// Authenticates via API key (`X-Api-Key`) or JWT Bearer token.
    /// 
    /// Example payload for an email notification:
    /// ```
    /// {
    ///   "recipientEmail": "user@example.com",
    ///   "type": "transactional",
    ///   "channel": "email",
    ///   "payload": "{\"subject\":\"Welcome!\",\"html\":\"&lt;h1&gt;Hello&lt;/h1&gt;\"}"
    /// }
    /// ```
    /// 
    /// The `payload` field is a JSON string containing provider-specific fields:
    /// - For email: `subject`, `html` (or `text`)
    /// - The `type` field is a free-form string (e.g. "transactional", "marketing", "alert")
    /// 
    /// Use the optional `Idempotency-Key` header to prevent duplicate sends.
    /// </remarks>
    /// <param name="request">Notification details</param>
    /// <param name="idempotencyKey">Optional idempotency key to prevent duplicate sends</param>
    /// <returns>The public ID of the created notification</returns>
    /// <response code="200">Notification created and queued for delivery</response>
    /// <response code="409">Duplicate request (idempotency key already used)</response>
    [HttpPost]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(typeof(object), 409)]
    public async Task<IActionResult> Create(
        [FromBody] CreateNotificationRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        if (!_currentOrg.IsAuthenticated || _currentOrg.OrganizationId is null)
            return Unauthorized(new { error = "No organization context." });

        if (_currentUser.UserId is null)
            return Unauthorized(new { error = "No user context." });

        var command = new CreateNotificationCommand(
            _currentOrg.OrganizationId.Value,
            _currentUser.UserId.Value,
            request.RecipientEmail,
            request.Type,
            request.Channel,
            request.Payload,
            idempotencyKey);

        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
            return Conflict(new { error = result.Error });

        return Ok(new { publicId = result.Value });
    }

    /// <summary>
    /// List notifications with optional filtering and pagination.
    /// </summary>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Items per page (1-100, default: 20)</param>
    /// <param name="dateFrom">Filter notifications created after this date</param>
    /// <param name="dateTo">Filter notifications created before this date</param>
    /// <param name="status">Filter by status: Pending, Processing, Sent, Failed, Retrying, DeadLetter</param>
    /// <returns>Paginated list of notifications</returns>
    [HttpGet]
    [ProducesResponseType(typeof(object), 200)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null,
        [FromQuery] string? status = null,
        CancellationToken cancellationToken = default)
    {
        if (!_currentOrg.IsAuthenticated || _currentOrg.OrganizationId is null)
            return Unauthorized(new { error = "No organization context." });

        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = new GetNotificationsQuery(
            _currentOrg.OrganizationId.Value, page, pageSize, dateFrom, dateTo, status);

        var result = await _mediator.Send(query, cancellationToken);

        return Ok(new
        {
            items = result.Value!.Items,
            totalCount = result.Value.TotalCount,
            pageNumber = result.Value.PageNumber,
            pageSize = result.Value.PageSize,
        });
    }

    /// <summary>
    /// Get detailed information about a specific notification.
    /// </summary>
    /// <param name="publicId">The notification's public ID</param>
    /// <returns>Full notification detail including delivery logs</returns>
    [HttpGet("{publicId:guid}")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(
        Guid publicId,
        CancellationToken cancellationToken)
    {
        if (!_currentOrg.IsAuthenticated || _currentOrg.OrganizationId is null)
            return Unauthorized(new { error = "No organization context." });

        var detail = await _notificationService.GetDetailAsync(
            _currentOrg.OrganizationId.Value, publicId, cancellationToken);

        if (detail is null)
            return NotFound(new { error = "Notification not found." });

        return Ok(detail);
    }

    /// <summary>
    /// Get delivery logs for a specific notification.
    /// </summary>
    /// <param name="publicId">The notification's public ID</param>
    /// <returns>List of delivery log entries</returns>
    [HttpGet("{publicId:guid}/logs")]
    [ProducesResponseType(typeof(object), 200)]
    public async Task<IActionResult> GetLogs(
        Guid publicId,
        CancellationToken cancellationToken)
    {
        if (!_currentOrg.IsAuthenticated || _currentOrg.OrganizationId is null)
            return Unauthorized(new { error = "No organization context." });

        var logs = await _notificationService.GetLogsAsync(
            _currentOrg.OrganizationId.Value, publicId, cancellationToken);

        return Ok(logs);
    }

    /// <summary>
    /// Retry a failed notification.
    /// </summary>
    /// <param name="publicId">The notification's public ID</param>
    /// <returns>Confirmation of retry</returns>
    [HttpPost("{publicId:guid}/retry")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(typeof(object), 400)]
    public async Task<IActionResult> Retry(
        Guid publicId,
        CancellationToken cancellationToken)
    {
        if (!_currentOrg.IsAuthenticated || _currentOrg.OrganizationId is null)
            return Unauthorized(new { error = "No organization context." });

        var success = await _notificationService.RetryAsync(
            _currentOrg.OrganizationId.Value, publicId, cancellationToken);

        if (!success)
            return BadRequest(new { error = "Notification cannot be retried in its current state." });

        return Ok(new { retried = true });
    }
}

/// <summary>
/// Request body for creating a notification.
/// </summary>
public record CreateNotificationRequest(
    /// <summary>Recipient email address</summary>
    string RecipientEmail,
    /// <summary>Notification type (e.g. "transactional", "marketing", "alert")</summary>
    string Type,
    /// <summary>Delivery channel: Email, Sms, Push, or InApp</summary>
    NotificationHub.Domain.Enums.NotificationChannel Channel,
    /// <summary>JSON string with provider-specific payload. For email: {"subject":"...", "html":"...", "text":"..."}</summary>
    string Payload
);
