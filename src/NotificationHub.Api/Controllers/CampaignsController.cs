using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NotificationHub.Application.Abstractions;
using NotificationHub.Shared.Abstractions;
using NotificationHub.Infrastructure.Persistence;
using NotificationHub.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace NotificationHub.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/campaigns")]
[Authorize]
public class CampaignsController : ControllerBase
{
    private readonly ICampaignService _campaignService;
    private readonly ICampaignRepository _campaignRepository;
    private readonly ICurrentOrganization _currentOrg;
    private readonly ICurrentUser _currentUser;
    private readonly AppDbContext _context;

    public CampaignsController(
        ICampaignService campaignService,
        ICampaignRepository campaignRepository,
        ICurrentOrganization currentOrg,
        ICurrentUser currentUser,
        AppDbContext context)
    {
        _campaignService = campaignService;
        _campaignRepository = campaignRepository;
        _currentOrg = currentOrg;
        _currentUser = currentUser;
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (_currentOrg.OrganizationId is null)
            return Unauthorized(new { error = "No organization context." });

        var (items, totalCount) = await _campaignRepository.GetPagedAsync(
            _currentOrg.OrganizationId.Value, page, pageSize, cancellationToken);

        return Ok(new
        {
            items = items.Select(c => new
            {
                c.Id, c.Title, c.Subject, c.Channel, c.Status,
                c.TotalRecipients, c.ScheduledAt, c.StartedAt,
                c.CompletedAt, c.CreatedAt,
                recipientCount = c.Recipients.Count,
            }),
            totalCount, pageNumber = page, pageSize,
        });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        if (_currentOrg.OrganizationId is null)
            return Unauthorized(new { error = "No organization context." });

        var campaign = await _campaignRepository.GetByIdAsync(
            id, _currentOrg.OrganizationId.Value, cancellationToken);

        if (campaign is null)
            return NotFound(new { error = "Campaign not found." });

        var sent = campaign.Recipients.Count(r => r.NotificationId != null);
        var pending = campaign.Recipients.Count(r => r.NotificationId == null);

        return Ok(new
        {
            campaign.Id, campaign.Title, campaign.Subject, campaign.Message,
            campaign.Channel, campaign.Status, campaign.TotalRecipients,
            campaign.ScheduledAt, campaign.StartedAt, campaign.CompletedAt,
            campaign.CreatedByUserId, campaign.CreatedAt,
            stats = new { sent, pending, total = campaign.Recipients.Count },
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateCampaignRequest request,
        CancellationToken cancellationToken)
    {
        if (_currentOrg.OrganizationId is null)
            return Unauthorized(new { error = "No organization context." });

        try
        {
            var campaign = await _campaignService.CreateAsync(new(
                _currentOrg.OrganizationId.Value,
                _currentUser.UserId,
                request.Title,
                request.Subject,
                request.Body,
                request.TemplateId,
                request.Channel,
                request.ScheduledAt
            ), cancellationToken);

            return Ok(new { campaign.Id, campaign.Title, campaign.Status });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
    
    [HttpGet("{id:guid}/progress")]
    public async Task<IActionResult> GetProgress(Guid id, CancellationToken cancellationToken)
    {
        if (_currentOrg.OrganizationId is null)
            return Unauthorized(new { error = "No organization context." });

        var campaign = await _campaignRepository.GetByIdAsync(
            id, _currentOrg.OrganizationId.Value, cancellationToken);

        if (campaign is null)
            return NotFound(new { error = "Campaign not found." });

        var orgId = _currentOrg.OrganizationId.Value;

        // Recipients not yet turned into notifications
        var unqueued = campaign.Recipients.Count(r => r.NotificationId == null);

        // Notifications linked to this campaign
        var notifQuery = _context.Notifications
            .Where(n => n.OrganizationId == orgId)
            .Where(n => _context.CampaignRecipients
                .Where(r => r.CampaignId == id && r.NotificationId != null)
                .Select(r => r.NotificationId)
                .Contains(n.Id));

        var pending      = await notifQuery.CountAsync(n => n.Status == NotificationStatus.Pending, cancellationToken);
        var processing   = await notifQuery.CountAsync(n => n.Status == NotificationStatus.Processing, cancellationToken);
        var retrying     = await notifQuery.CountAsync(n => n.Status == NotificationStatus.Retrying, cancellationToken);
        var sent         = await notifQuery.CountAsync(n => n.Status == NotificationStatus.Sent, cancellationToken);
        var failed       = await notifQuery.CountAsync(n => n.Status == NotificationStatus.Failed, cancellationToken);
        var deadLetter   = await notifQuery.CountAsync(n => n.Status == NotificationStatus.DeadLetter, cancellationToken);

        var total = campaign.TotalRecipients;
        var processed = sent + failed + deadLetter;
        var progressPct = total > 0 ? Math.Round((double)processed / total * 100, 1) : 0;

        return Ok(new
        {
            campaignId = id,
            status = campaign.Status.ToString(),
            total,
            unqueued,
            pending,
            processing,
            retrying,
            sent,
            failed,
            deadLetter,
            progressPercent = progressPct,
            startedAt = campaign.StartedAt,
            completedAt = campaign.CompletedAt,
        });
    }
    [HttpPost("{id:guid}/recipients")]
    public async Task<IActionResult> AddRecipients(
        Guid id,
        [FromBody] AddRecipientsRequest request,
        CancellationToken cancellationToken)
    {
        if (_currentOrg.OrganizationId is null)
            return Unauthorized(new { error = "No organization context." });

        try
        {
            var result = await _campaignService.AddRecipientsAsync(new(
                id, _currentOrg.OrganizationId.Value, request.Emails
            ), cancellationToken);

            return Ok(new { added = result.Added, skipped = result.Skipped });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{id:guid}/send")]
    public async Task<IActionResult> Send(Guid id, CancellationToken cancellationToken)
    {
        if (_currentOrg.OrganizationId is null)
            return Unauthorized(new { error = "No organization context." });

        try
        {
            await _campaignService.SendAsync(
                id, _currentOrg.OrganizationId.Value, _currentUser.UserId, cancellationToken);
            return Ok(new { started = true, campaignId = id });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{id:guid}/schedule")]
    public async Task<IActionResult> Schedule(
        Guid id,
        [FromBody] ScheduleCampaignRequest request,
        CancellationToken cancellationToken)
    {
        if (_currentOrg.OrganizationId is null)
            return Unauthorized(new { error = "No organization context." });

        try
        {
            await _campaignService.ScheduleAsync(
                id, _currentOrg.OrganizationId.Value, request.ScheduledAt, cancellationToken);
            return Ok(new { scheduled = true, scheduledAt = request.ScheduledAt });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("{id:guid}/notifications")]
    public async Task<IActionResult> GetNotifications(
        Guid id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (_currentOrg.OrganizationId is null)
            return Unauthorized(new { error = "No organization context." });

        var campaign = await _campaignRepository.GetByIdAsync(
            id, _currentOrg.OrganizationId.Value, cancellationToken);

        if (campaign is null)
            return NotFound(new { error = "Campaign not found." });

        var query = _context.Notifications
            .Where(n => n.OrganizationId == _currentOrg.OrganizationId.Value)
            .Where(n => _context.CampaignRecipients
                .Where(r => r.CampaignId == id && r.NotificationId != null)
                .Select(r => r.NotificationId)
                .Contains(n.Id))
            .OrderByDescending(n => n.CreatedAt);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(n => new
            {
                n.PublicId, n.RecipientEmail, n.Type,
                channel = n.Channel.ToString(),
                status = n.Status.ToString(),
                n.RetryCount, n.CreatedAt
            })
            .ToListAsync(cancellationToken);

        var failedCount = await query
            .CountAsync(n =>
                n.Status == NotificationStatus.DeadLetter ||
                n.Status == NotificationStatus.Failed, cancellationToken);

        return Ok(new { items, totalCount = total, pageNumber = page, pageSize, failedCount });
    }

    [HttpPost("{id:guid}/pause")]
    public async Task<IActionResult> Pause(Guid id, CancellationToken cancellationToken)
    {
        if (_currentOrg.OrganizationId is null)
            return Unauthorized(new { error = "No organization context." });

        try
        {
            await _campaignService.PauseAsync(id, _currentOrg.OrganizationId.Value, cancellationToken);
            return Ok(new { paused = true });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{id:guid}/resume")]
    public async Task<IActionResult> Resume(Guid id, CancellationToken cancellationToken)
    {
        if (_currentOrg.OrganizationId is null)
            return Unauthorized(new { error = "No organization context." });

        try
        {
            await _campaignService.ResumeAsync(id, _currentOrg.OrganizationId.Value, cancellationToken);
            return Ok(new { resumed = true });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        if (_currentOrg.OrganizationId is null)
            return Unauthorized(new { error = "No organization context." });

        try
        {
            await _campaignService.DeleteAsync(id, _currentOrg.OrganizationId.Value, cancellationToken);
            return Ok(new { deleted = true });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}

public record CreateCampaignRequest(
    string Title, string Subject, string? Body,
    Guid? TemplateId, NotificationChannel Channel, DateTime? ScheduledAt);
public record AddRecipientsRequest(List<string> Emails);
public record ScheduleCampaignRequest(DateTime ScheduledAt);