using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NotificationHub.Application.Abstractions;
using NotificationHub.Domain.Entities;
using NotificationHub.Domain.Enums;
using NotificationHub.Shared.Abstractions;
using NotificationHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace NotificationHub.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/campaigns")]
[Authorize]
public class CampaignsController : ControllerBase
{
    private readonly ICampaignRepository _campaignRepository;
    private readonly ITemplateRepository _templateRepository;
    private readonly IOrganizationMemberRepository _memberRepository;
    private readonly IUserRepository _userRepository;
    private readonly IEmailProvider _emailProvider;
    private readonly ICurrentOrganization _currentOrg;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;
    private readonly AppDbContext _context;

    public CampaignsController(
        ICampaignRepository campaignRepository,
        ITemplateRepository templateRepository,
        IOrganizationMemberRepository memberRepository,
        IUserRepository userRepository,
        IEmailProvider emailProvider,
        AppDbContext context,
        ICurrentOrganization currentOrg,
        ICurrentUser currentUser,
        IClock clock)
    {
        _campaignRepository = campaignRepository;
        _templateRepository = templateRepository;
        _memberRepository = memberRepository;
        _userRepository = userRepository;
        _emailProvider = emailProvider;
        _currentOrg = currentOrg;
        _currentUser = currentUser;
        _context = context;
        _clock = clock;
    }

    // Shared helper — sends an email to every active org member
    private async Task NotifyOrgMembersAsync(
        Guid orgId,
        string subject,
        string html,
        string text,
        CancellationToken cancellationToken)
    {
        var members = await _memberRepository.GetByOrgAsync(orgId, cancellationToken);
        foreach (var member in members.Where(m => m.Role != "revoked" && m.User?.Email != null))
        {
            try
            {
                await _emailProvider.SendAsync(new EmailMessage(
                    From: "NotificationHub <noreply@coursevaultai.app>",
                    To: member.User!.Email,
                    Subject: subject,
                    Html: html,
                    Text: text
                ), cancellationToken);
            }
            catch
            {
                // Never block the main action because an email failed
            }
        }
    }

    private static string StripHtml(string html)
    {
        var stripped = System.Text.RegularExpressions.Regex.Replace(html ?? "", "<[^>]+>", "");
        return stripped.Length > 300 ? stripped[..300] + "..." : stripped;
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
                c.StartedAt,
                c.CompletedAt,
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
            campaign.StartedAt,
            campaign.CompletedAt,
            campaign.CreatedByUserId,
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

        // Load creator info for the email
        var creator = _currentUser.UserId.HasValue
            ? await _userRepository.GetByIdAsync(_currentUser.UserId.Value, cancellationToken)
            : null;

        var creatorName = creator?.FullName ?? "A team member";
        var creatorEmail = creator?.Email ?? "—";
        var bodyPreview = StripHtml(body);
        var scheduledNote = request.ScheduledAt.HasValue
            ? $"<tr style='background:#f9f9f9'><td style='padding:8px;color:#888;width:140px'>Scheduled for</td><td style='padding:8px'>{request.ScheduledAt.Value:dddd, MMMM d yyyy 'at' h:mm tt}</td></tr>"
            : "";

        _ = Task.Run(() => NotifyOrgMembersAsync(
            orgId,
            subject: $"New campaign created: {campaign.Title}",
            html: $"""
                <div style="font-family:sans-serif;max-width:600px;margin:0 auto">
                  <h2 style="color:#7c3aed">New Campaign Created 📋</h2>
                  <p><strong>{creatorName}</strong> created a new campaign draft.</p>
                  <table style="width:100%;border-collapse:collapse;margin:20px 0">
                    <tr><td style="padding:8px;color:#888;width:140px">Campaign</td><td style="padding:8px;font-weight:600">{campaign.Title}</td></tr>
                    <tr style="background:#f9f9f9"><td style="padding:8px;color:#888">Subject</td><td style="padding:8px">{campaign.Subject}</td></tr>
                    <tr><td style="padding:8px;color:#888">Created by</td><td style="padding:8px">{creatorName} ({creatorEmail})</td></tr>
                    <tr style="background:#f9f9f9"><td style="padding:8px;color:#888">Creator ID</td><td style="padding:8px;font-family:monospace;font-size:12px">{_currentUser.UserId}</td></tr>
                    <tr><td style="padding:8px;color:#888">Status</td><td style="padding:8px">Draft</td></tr>
                    {scheduledNote}
                  </table>
                  <div style="background:#f5f5f5;border-left:4px solid #7c3aed;padding:16px;margin:20px 0;border-radius:4px">
                    <p style="margin:0 0 8px;color:#888;font-size:12px;text-transform:uppercase;letter-spacing:1px">Message Preview</p>
                    <p style="margin:0;color:#333;font-size:14px;line-height:1.6">{bodyPreview}</p>
                  </div>
                  <p style="color:#888;font-size:12px">You're receiving this because you're a member of this organization on NotificationHub.</p>
                </div>
                """,
            text: $"New campaign created: {campaign.Title}\nCreated by: {creatorName} ({creatorEmail})\nSubject: {campaign.Subject}\nStatus: Draft\n\nPreview:\n{bodyPreview}",
            cancellationToken: CancellationToken.None
        ), cancellationToken);

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

        var creator = _currentUser.UserId.HasValue
            ? await _userRepository.GetByIdAsync(_currentUser.UserId.Value, cancellationToken)
            : null;

        var creatorName = creator?.FullName ?? "A team member";
        var creatorEmail = creator?.Email ?? "—";
        var bodyPreview = StripHtml(campaign.Message);
        var startedTime = campaign.StartedAt?.ToString("h:mm tt, MMM d yyyy") ?? "now";

        _ = Task.Run(() => NotifyOrgMembersAsync(
            _currentOrg.OrganizationId.Value,
            subject: $"Campaign sending now: {campaign.Title}",
            html: $"""
                <div style="font-family:sans-serif;max-width:600px;margin:0 auto">
                  <h2 style="color:#0d9488">Campaign Sending Now </h2>
                  <p>The campaign <strong>{campaign.Title}</strong> is now sending.</p>
                  <table style="width:100%;border-collapse:collapse;margin:20px 0">
                    <tr><td style="padding:8px;color:#888;width:140px">Campaign</td><td style="padding:8px;font-weight:600">{campaign.Title}</td></tr>
                    <tr style="background:#f9f9f9"><td style="padding:8px;color:#888">Subject</td><td style="padding:8px">{campaign.Subject}</td></tr>
                    <tr><td style="padding:8px;color:#888">Started at</td><td style="padding:8px">{startedTime}</td></tr>
                    <tr style="background:#f9f9f9"><td style="padding:8px;color:#888">Recipients</td><td style="padding:8px">{campaign.TotalRecipients}</td></tr>
                    <tr><td style="padding:8px;color:#888">Sent by</td><td style="padding:8px">{creatorName} ({creatorEmail})</td></tr>
                    <tr style="background:#f9f9f9"><td style="padding:8px;color:#888">Sender ID</td><td style="padding:8px;font-family:monospace;font-size:12px">{_currentUser.UserId}</td></tr>
                  </table>
                  <div style="background:#f5f5f5;border-left:4px solid #0d9488;padding:16px;margin:20px 0;border-radius:4px">
                    <p style="margin:0 0 8px;color:#888;font-size:12px;text-transform:uppercase;letter-spacing:1px">Message Preview</p>
                    <p style="margin:0;color:#333;font-size:14px;line-height:1.6">{bodyPreview}</p>
                  </div>
                  <p style="color:#888;font-size:12px">You're receiving this because you're a member of this organization on NotificationHub.</p>
                </div>
                """,
            text: $"Campaign sending: {campaign.Title}\nStarted: {startedTime}\nRecipients: {campaign.TotalRecipients}\nSent by: {creatorName} ({creatorEmail})\n\nPreview:\n{bodyPreview}",
            cancellationToken: CancellationToken.None
        ), cancellationToken);

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
                n.PublicId,
                n.RecipientEmail,
                n.Type,
                channel = n.Channel.ToString(),
                status = n.Status.ToString(),
                n.RetryCount,
                n.CreatedAt
            })
            .ToListAsync(cancellationToken);

        var failedCount = await _context.Notifications
            .Where(n => n.OrganizationId == _currentOrg.OrganizationId.Value)
            .Where(n => _context.CampaignRecipients
                .Where(r => r.CampaignId == id && r.NotificationId != null)
                .Select(r => r.NotificationId)
                .Contains(n.Id))
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