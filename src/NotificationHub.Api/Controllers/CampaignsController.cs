using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NotificationHub.Application.Abstractions;
using NotificationHub.Domain.Entities;
using NotificationHub.Domain.Enums;
using NotificationHub.Shared.Abstractions;

namespace NotificationHub.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/campaigns")]
[Authorize]
public class CampaignsController : ControllerBase
{
    private readonly ICampaignRepository _campaignRepository;
    private readonly ITemplateRepository _templateRepository;
    private readonly ICurrentOrganization _currentOrg;
    private readonly IClock _clock;

    public CampaignsController(
        ICampaignRepository campaignRepository,
        ITemplateRepository templateRepository,
        ICurrentOrganization currentOrg,
        IClock clock)
    {
        _campaignRepository = campaignRepository;
        _templateRepository = templateRepository;
        _currentOrg = currentOrg;
        _clock = clock;
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
                c.Id,
                c.Title,
                c.Subject,
                c.Channel,
                c.Status,
                c.TotalRecipients,
                c.ScheduledAt,
                c.CreatedAt,
                recipientCount = c.Recipients.Count,
            }),
            totalCount,
            pageNumber = page,
            pageSize,
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
            campaign.Id,
            campaign.Title,
            campaign.Subject,
            campaign.Message,
            campaign.Channel,
            campaign.Status,
            campaign.TotalRecipients,
            campaign.ScheduledAt,
            campaign.CreatedAt,
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

        var orgId = _currentOrg.OrganizationId.Value;
        var body = request.Body;

        // If templateId provided, pull body from template
        if (request.TemplateId.HasValue)
        {
            var template = await _templateRepository.GetByIdAsync(
                request.TemplateId.Value, orgId, cancellationToken);
            if (template is null)
                return BadRequest(new { error = "Template not found." });
            body = template.Body;
        }

        if (string.IsNullOrWhiteSpace(body))
            return BadRequest(new { error = "Body or TemplateId is required." });

        var campaign = new Campaign
        {
            OrganizationId = orgId,
            Title = request.Title,
            Subject = request.Subject,
            Message = body,
            Channel = request.Channel,
            Status = CampaignStatus.Draft,
            ScheduledAt = request.ScheduledAt,
        };

        await _campaignRepository.AddAsync(campaign, cancellationToken);
        await _campaignRepository.SaveChangesAsync(cancellationToken);

        return Ok(new { campaign.Id, campaign.Title, campaign.Status });
    }

    [HttpPost("{id:guid}/recipients")]
    public async Task<IActionResult> AddRecipients(
        Guid id,
        [FromBody] AddRecipientsRequest request,
        CancellationToken cancellationToken)
    {
        if (_currentOrg.OrganizationId is null)
            return Unauthorized(new { error = "No organization context." });

        var campaign = await _campaignRepository.GetByIdAsync(
            id, _currentOrg.OrganizationId.Value, cancellationToken);

        if (campaign is null)
            return NotFound(new { error = "Campaign not found." });

        if (campaign.Status != CampaignStatus.Draft)
            return BadRequest(new { error = "Can only add recipients to a draft campaign." });

        // Parse emails — support comma, newline, semicolon separated
        var emails = request.Emails
            .SelectMany(e => e.Split(new[] { ',', '\n', '\r', ';' }, StringSplitOptions.RemoveEmptyEntries))
            .Select(e => e.Trim().ToLowerInvariant())
            .Where(e => e.Contains('@'))
            .Distinct()
            .ToList();

        if (emails.Count == 0)
            return BadRequest(new { error = "No valid email addresses found." });

        var newRecipients = new List<CampaignRecipient>();
        foreach (var email in emails)
        {
            var exists = await _campaignRepository.RecipientExistsAsync(id, email, cancellationToken);
            if (!exists)
            {
                newRecipients.Add(new CampaignRecipient
                {
                    CampaignId = id,
                    OrganizationId = _currentOrg.OrganizationId.Value,
                    RecipientEmail = email,
                });
            }
        }

        if (newRecipients.Count > 0)
        {
            await _campaignRepository.AddRecipientsAsync(newRecipients, cancellationToken);
            campaign.TotalRecipients += newRecipients.Count;
            await _campaignRepository.SaveChangesAsync(cancellationToken);
        }

        return Ok(new { added = newRecipients.Count, skipped = emails.Count - newRecipients.Count });
    }

    [HttpPost("{id:guid}/send")]
    public async Task<IActionResult> Send(Guid id, CancellationToken cancellationToken)
    {
        if (_currentOrg.OrganizationId is null)
            return Unauthorized(new { error = "No organization context." });

        var campaign = await _campaignRepository.GetByIdAsync(
            id, _currentOrg.OrganizationId.Value, cancellationToken);

        if (campaign is null)
            return NotFound(new { error = "Campaign not found." });

        if (campaign.Status != CampaignStatus.Draft && campaign.Status != CampaignStatus.Paused)
            return BadRequest(new { error = "Campaign must be in Draft or Paused state to send." });

        if (campaign.TotalRecipients == 0)
            return BadRequest(new { error = "Add recipients before sending." });

        campaign.Status = CampaignStatus.Running;
        await _campaignRepository.SaveChangesAsync(cancellationToken);

        return Ok(new { started = true, campaignId = campaign.Id });
    }

    [HttpPost("{id:guid}/schedule")]
    public async Task<IActionResult> Schedule(
        Guid id,
        [FromBody] ScheduleCampaignRequest request,
        CancellationToken cancellationToken)
    {
        if (_currentOrg.OrganizationId is null)
            return Unauthorized(new { error = "No organization context." });

        var campaign = await _campaignRepository.GetByIdAsync(
            id, _currentOrg.OrganizationId.Value, cancellationToken);

        if (campaign is null)
            return NotFound(new { error = "Campaign not found." });

        if (campaign.Status != CampaignStatus.Draft)
            return BadRequest(new { error = "Only draft campaigns can be scheduled." });

        if (request.ScheduledAt <= _clock.UtcNow)
            return BadRequest(new { error = "Scheduled time must be in the future." });

        if (campaign.TotalRecipients == 0)
            return BadRequest(new { error = "Add recipients before scheduling." });

        campaign.Status = CampaignStatus.Scheduled;
        campaign.ScheduledAt = request.ScheduledAt;
        await _campaignRepository.SaveChangesAsync(cancellationToken);

        return Ok(new { scheduled = true, scheduledAt = campaign.ScheduledAt });
    }

    [HttpPost("{id:guid}/pause")]
    public async Task<IActionResult> Pause(Guid id, CancellationToken cancellationToken)
    {
        if (_currentOrg.OrganizationId is null)
            return Unauthorized(new { error = "No organization context." });

        var campaign = await _campaignRepository.GetByIdAsync(
            id, _currentOrg.OrganizationId.Value, cancellationToken);

        if (campaign is null)
            return NotFound(new { error = "Campaign not found." });

        if (campaign.Status != CampaignStatus.Running)
            return BadRequest(new { error = "Only running campaigns can be paused." });

        campaign.Status = CampaignStatus.Paused;
        await _campaignRepository.SaveChangesAsync(cancellationToken);

        return Ok(new { paused = true });
    }

    [HttpPost("{id:guid}/resume")]
    public async Task<IActionResult> Resume(Guid id, CancellationToken cancellationToken)
    {
        if (_currentOrg.OrganizationId is null)
            return Unauthorized(new { error = "No organization context." });

        var campaign = await _campaignRepository.GetByIdAsync(
            id, _currentOrg.OrganizationId.Value, cancellationToken);

        if (campaign is null)
            return NotFound(new { error = "Campaign not found." });

        if (campaign.Status != CampaignStatus.Paused)
            return BadRequest(new { error = "Only paused campaigns can be resumed." });

        campaign.Status = CampaignStatus.Running;
        await _campaignRepository.SaveChangesAsync(cancellationToken);

        return Ok(new { resumed = true });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        if (_currentOrg.OrganizationId is null)
            return Unauthorized(new { error = "No organization context." });

        var campaign = await _campaignRepository.GetByIdAsync(
            id, _currentOrg.OrganizationId.Value, cancellationToken);

        if (campaign is null)
            return NotFound(new { error = "Campaign not found." });

        if (campaign.Status == CampaignStatus.Running)
            return BadRequest(new { error = "Pause the campaign before deleting." });

        campaign.DeletedAt = _clock.UtcNow;
        await _campaignRepository.SaveChangesAsync(cancellationToken);

        return Ok(new { deleted = true });
    }
}

public record CreateCampaignRequest(
    string Title,
    string Subject,
    string? Body,
    Guid? TemplateId,
    NotificationChannel Channel,
    DateTime? ScheduledAt
);
public record AddRecipientsRequest(List<string> Emails);
public record ScheduleCampaignRequest(DateTime ScheduledAt);