using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NotificationHub.Application.Abstractions;
using NotificationHub.Domain.Entities;
using NotificationHub.Domain.Enums;
using NotificationHub.Shared.Abstractions;
using NotificationHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace NotificationHub.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/campaigns")]
[Authorize]
public class CampaignsController : ControllerBase
{
    private readonly ICampaignRepository _campaignRepository;
    private readonly ITemplateRepository _templateRepository;
    private readonly IUserRepository _userRepository;
    private readonly IOrgNotificationService _orgNotifier;
    private readonly ICurrentOrganization _currentOrg;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;
    private readonly AppDbContext _context;

    public CampaignsController(
        ICampaignRepository campaignRepository,
        ITemplateRepository templateRepository,
        IUserRepository userRepository,
        IOrgNotificationService orgNotifier,
        AppDbContext context,
        ICurrentOrganization currentOrg,
        ICurrentUser currentUser,
        IClock clock)
    {
        _campaignRepository = campaignRepository;
        _templateRepository = templateRepository;
        _userRepository = userRepository;
        _orgNotifier = orgNotifier;
        _currentOrg = currentOrg;
        _currentUser = currentUser;
        _context = context;
        _clock = clock;
    }

    private static string StripHtml(string html)
    {
        var stripped = System.Text.RegularExpressions.Regex.Replace(html ?? "", "<[^>]+>", "");
        return stripped.Length > 300 ? stripped[..300] + "..." : stripped;
    }

    private static string BuildCreatedEmail(
        string title, string subject, string creatorName,
        string creatorEmail, string creatorId, string bodyPreview,
        string? scheduledTime) => $"""
        <div style="font-family:sans-serif;max-width:600px;margin:0 auto;color:#1a1a2e">
          <div style="background:#7c3aed;padding:32px;border-radius:12px 12px 0 0">
            <h1 style="margin:0;color:white;font-size:22px;font-weight:700"> New Campaign Created</h1>
            <p style="margin:8px 0 0;color:rgba(255,255,255,0.8);font-size:14px">A new campaign draft is ready</p>
          </div>
          <div style="background:#fafafa;border:1px solid #eee;border-top:none;border-radius:0 0 12px 12px;padding:32px">
            <table style="width:100%;border-collapse:collapse;margin-bottom:24px">
              <tr><td style="padding:10px 12px;color:#888;font-size:13px;width:140px;border-bottom:1px solid #f0f0f0">Campaign</td><td style="padding:10px 12px;font-weight:600;border-bottom:1px solid #f0f0f0">{title}</td></tr>
              <tr><td style="padding:10px 12px;color:#888;font-size:13px;border-bottom:1px solid #f0f0f0;background:#fff">Subject</td><td style="padding:10px 12px;border-bottom:1px solid #f0f0f0;background:#fff">{subject}</td></tr>
              <tr><td style="padding:10px 12px;color:#888;font-size:13px;border-bottom:1px solid #f0f0f0">Created by</td><td style="padding:10px 12px;border-bottom:1px solid #f0f0f0">{creatorName} <span style="color:#888;font-size:12px">({creatorEmail})</span></td></tr>
              <tr><td style="padding:10px 12px;color:#888;font-size:13px;border-bottom:1px solid #f0f0f0;background:#fff">Creator ID</td><td style="padding:10px 12px;font-size:12px;font-family:monospace;color:#888;border-bottom:1px solid #f0f0f0;background:#fff">{creatorId}</td></tr>
              <tr><td style="padding:10px 12px;color:#888;font-size:13px;border-bottom:1px solid #f0f0f0">Status</td><td style="padding:10px 12px;border-bottom:1px solid #f0f0f0"><span style="background:#f0f0f0;color:#666;padding:2px 10px;border-radius:20px;font-size:12px">Draft</span></td></tr>
              {(scheduledTime != null ? $"<tr><td style=\"padding:10px 12px;color:#888;font-size:13px;background:#fff\">Scheduled for</td><td style=\"padding:10px 12px;font-weight:600;color:#7c3aed;background:#fff\">{scheduledTime}</td></tr>" : "")}
            </table>
            <div style="background:white;border:1px solid #e8e8e8;border-left:4px solid #7c3aed;border-radius:0 8px 8px 0;padding:16px 20px">
              <p style="margin:0 0 8px;color:#888;font-size:11px;text-transform:uppercase;letter-spacing:1px;font-weight:600">Message Preview</p>
              <p style="margin:0;color:#444;font-size:14px;line-height:1.7">{bodyPreview}</p>
            </div>
            <p style="color:#bbb;font-size:11px;margin-top:24px;text-align:center">You're receiving this because you're a member of this organization on NotificationHub.</p>
          </div>
        </div>
        """;

    private static string BuildSendingNowEmail(
        string title, string subject, string senderName,
        string senderEmail, string senderId, int recipients,
        string startedTime, string bodyPreview) => $"""
        <div style="font-family:sans-serif;max-width:600px;margin:0 auto;color:#1a1a2e">
          <div style="background:#7c3aed;padding:32px;border-radius:12px 12px 0 0">
            <h1 style="margin:0;color:white;font-size:22px;font-weight:700"> Campaign Sending Now</h1>
            <p style="margin:8px 0 0;color:rgba(255,255,255,0.8);font-size:14px">Emails are being delivered to recipients</p>
          </div>
          <div style="background:#fafafa;border:1px solid #eee;border-top:none;border-radius:0 0 12px 12px;padding:32px">
            <table style="width:100%;border-collapse:collapse;margin-bottom:24px">
              <tr><td style="padding:10px 12px;color:#888;font-size:13px;width:140px;border-bottom:1px solid #f0f0f0">Campaign</td><td style="padding:10px 12px;font-weight:600;border-bottom:1px solid #f0f0f0">{title}</td></tr>
              <tr><td style="padding:10px 12px;color:#888;font-size:13px;border-bottom:1px solid #f0f0f0;background:#fff">Subject</td><td style="padding:10px 12px;border-bottom:1px solid #f0f0f0;background:#fff">{subject}</td></tr>
              <tr><td style="padding:10px 12px;color:#888;font-size:13px;border-bottom:1px solid #f0f0f0">Started at</td><td style="padding:10px 12px;font-weight:600;color:#7c3aed;border-bottom:1px solid #f0f0f0">{startedTime}</td></tr>
              <tr><td style="padding:10px 12px;color:#888;font-size:13px;border-bottom:1px solid #f0f0f0;background:#fff">Recipients</td><td style="padding:10px 12px;border-bottom:1px solid #f0f0f0;background:#fff">{recipients:N0}</td></tr>
              <tr><td style="padding:10px 12px;color:#888;font-size:13px;border-bottom:1px solid #f0f0f0">Sent by</td><td style="padding:10px 12px;border-bottom:1px solid #f0f0f0">{senderName} <span style="color:#888;font-size:12px">({senderEmail})</span></td></tr>
              <tr><td style="padding:10px 12px;color:#888;font-size:13px;background:#fff">Sender ID</td><td style="padding:10px 12px;font-size:12px;font-family:monospace;color:#888;background:#fff">{senderId}</td></tr>
            </table>
            <div style="background:white;border:1px solid #e8e8e8;border-left:4px solid #7c3aed;border-radius:0 8px 8px 0;padding:16px 20px">
              <p style="margin:0 0 8px;color:#888;font-size:11px;text-transform:uppercase;letter-spacing:1px;font-weight:600">Message Preview</p>
              <p style="margin:0;color:#444;font-size:14px;line-height:1.7">{bodyPreview}</p>
            </div>
            <p style="color:#bbb;font-size:11px;margin-top:24px;text-align:center">You're receiving this because you're a member of this organization on NotificationHub.</p>
          </div>
        </div>
        """;

    private static string BuildScheduledEmail(
        string title, string subject, string scheduledTime,
        int recipients, string schedulerName, string schedulerEmail,
        string schedulerId, string bodyPreview) => $"""
        <div style="font-family:sans-serif;max-width:600px;margin:0 auto;color:#1a1a2e">
          <div style="background:#7c3aed;padding:32px;border-radius:12px 12px 0 0">
            <h1 style="margin:0;color:white;font-size:22px;font-weight:700"> Campaign Scheduled</h1>
            <p style="margin:8px 0 0;color:rgba(255,255,255,0.8);font-size:14px">This campaign will send automatically</p>
          </div>
          <div style="background:#fafafa;border:1px solid #eee;border-top:none;border-radius:0 0 12px 12px;padding:32px">
            <table style="width:100%;border-collapse:collapse;margin-bottom:24px">
              <tr><td style="padding:10px 12px;color:#888;font-size:13px;width:140px;border-bottom:1px solid #f0f0f0">Campaign</td><td style="padding:10px 12px;font-weight:600;border-bottom:1px solid #f0f0f0">{title}</td></tr>
              <tr><td style="padding:10px 12px;color:#888;font-size:13px;border-bottom:1px solid #f0f0f0;background:#fff">Subject</td><td style="padding:10px 12px;border-bottom:1px solid #f0f0f0;background:#fff">{subject}</td></tr>
              <tr><td style="padding:10px 12px;color:#888;font-size:13px;border-bottom:1px solid #f0f0f0">Sends at</td><td style="padding:10px 12px;font-weight:700;color:#7c3aed;font-size:15px;border-bottom:1px solid #f0f0f0">{scheduledTime}</td></tr>
              <tr><td style="padding:10px 12px;color:#888;font-size:13px;border-bottom:1px solid #f0f0f0;background:#fff">Recipients</td><td style="padding:10px 12px;border-bottom:1px solid #f0f0f0;background:#fff">{recipients:N0}</td></tr>
              <tr><td style="padding:10px 12px;color:#888;font-size:13px;border-bottom:1px solid #f0f0f0">Scheduled by</td><td style="padding:10px 12px;border-bottom:1px solid #f0f0f0">{schedulerName} <span style="color:#888;font-size:12px">({schedulerEmail})</span></td></tr>
              <tr><td style="padding:10px 12px;color:#888;font-size:13px;background:#fff">Scheduler ID</td><td style="padding:10px 12px;font-size:12px;font-family:monospace;color:#888;background:#fff">{schedulerId}</td></tr>
            </table>
            <div style="background:white;border:1px solid #e8e8e8;border-left:4px solid #7c3aed;border-radius:0 8px 8px 0;padding:16px 20px">
              <p style="margin:0 0 8px;color:#888;font-size:11px;text-transform:uppercase;letter-spacing:1px;font-weight:600">Message Preview</p>
              <p style="margin:0;color:#444;font-size:14px;line-height:1.7">{bodyPreview}</p>
            </div>
            <p style="color:#bbb;font-size:11px;margin-top:24px;text-align:center">You're receiving this because you're a member of this organization on NotificationHub.</p>
          </div>
        </div>
        """;

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

        var orgId = _currentOrg.OrganizationId.Value;
        var body = request.Body;

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
            CreatedByUserId = _currentUser.UserId,
            Title = request.Title,
            Subject = request.Subject,
            Message = body,
            Channel = request.Channel,
            Status = CampaignStatus.Draft,
            ScheduledAt = request.ScheduledAt,
        };

        await _campaignRepository.AddAsync(campaign, cancellationToken);
        await _campaignRepository.SaveChangesAsync(cancellationToken);

        var creator = _currentUser.UserId.HasValue
            ? await _userRepository.GetByIdAsync(_currentUser.UserId.Value, cancellationToken)
            : null;

        var creatorName = creator?.FullName ?? "A team member";
        var creatorEmail = creator?.Email ?? "—";
        var bodyPreview = StripHtml(body);
        var scheduledTime = request.ScheduledAt.HasValue
            ? request.ScheduledAt.Value.ToString("dddd, MMMM d yyyy 'at' h:mm tt")
            : null;

        await _orgNotifier.NotifyOrgAsync(
            orgId,
            $"New campaign created: {campaign.Title}",
            BuildCreatedEmail(campaign.Title, campaign.Subject, creatorName,
                creatorEmail, _currentUser.UserId?.ToString() ?? "—",
                bodyPreview, scheduledTime),
            cancellationToken);

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
        campaign.StartedAt = _clock.UtcNow;
        await _campaignRepository.SaveChangesAsync(cancellationToken);

        var sender = _currentUser.UserId.HasValue
            ? await _userRepository.GetByIdAsync(_currentUser.UserId.Value, cancellationToken)
            : null;

        var senderName = sender?.FullName ?? "A team member";
        var senderEmail = sender?.Email ?? "—";
        var startedTime = campaign.StartedAt?.ToString("h:mm tt, MMM d yyyy") ?? "now";
        var bodyPreview = StripHtml(campaign.Message);

        await _orgNotifier.NotifyOrgAsync(
            _currentOrg.OrganizationId.Value,
            $"Campaign sending now: {campaign.Title}",
            BuildSendingNowEmail(campaign.Title, campaign.Subject, senderName,
                senderEmail, _currentUser.UserId?.ToString() ?? "—",
                campaign.TotalRecipients, startedTime, bodyPreview),
            cancellationToken);

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

        var scheduler = _currentUser.UserId.HasValue
            ? await _userRepository.GetByIdAsync(_currentUser.UserId.Value, cancellationToken)
            : null;

        var schedulerName = scheduler?.FullName ?? "A team member";
        var schedulerEmail = scheduler?.Email ?? "—";
        var scheduledTime = campaign.ScheduledAt?.ToString("dddd, MMMM d yyyy 'at' h:mm tt") ?? "—";
        var bodyPreview = StripHtml(campaign.Message);

        await _orgNotifier.NotifyOrgAsync(
            _currentOrg.OrganizationId.Value,
            $"Campaign scheduled: {campaign.Title}",
            BuildScheduledEmail(campaign.Title, campaign.Subject, scheduledTime,
                campaign.TotalRecipients, schedulerName, schedulerEmail,
                _currentUser.UserId?.ToString() ?? "—", bodyPreview),
            cancellationToken);

        return Ok(new { scheduled = true, scheduledAt = campaign.ScheduledAt });
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

        var linkedIds = _context.CampaignRecipients
            .Where(r => r.CampaignId == id && r.NotificationId != null)
            .Select(r => r.NotificationId);

        var query = _context.Notifications
            .Where(n => n.OrganizationId == _currentOrg.OrganizationId.Value)
            .Where(n => linkedIds.Contains(n.Id))
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
            .CountAsync(n => n.Status == NotificationStatus.DeadLetter ||
                             n.Status == NotificationStatus.Failed, cancellationToken);

        return Ok(new { items, totalCount = total, pageNumber = page, pageSize, failedCount });
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
        campaign.StartedAt ??= _clock.UtcNow;
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