using NotificationHub.Application.Abstractions;
using NotificationHub.Domain.Entities;
using NotificationHub.Domain.Enums;
using NotificationHub.Shared.Abstractions;

namespace NotificationHub.Infrastructure.Services;

public class CampaignService : ICampaignService
{
    private readonly ICampaignRepository _campaignRepository;
    private readonly ITemplateRepository _templateRepository;
    private readonly IUserRepository _userRepository;
    private readonly IOrgNotificationService _orgNotifier;
    private readonly IClock _clock;

    public CampaignService(
        ICampaignRepository campaignRepository,
        ITemplateRepository templateRepository,
        IUserRepository userRepository,
        IOrgNotificationService orgNotifier,
        IClock clock)
    {
        _campaignRepository = campaignRepository;
        _templateRepository = templateRepository;
        _userRepository = userRepository;
        _orgNotifier = orgNotifier;
        _clock = clock;
    }

    public async Task<Campaign> CreateAsync(CreateCampaignDto dto, CancellationToken cancellationToken = default)
    {
        var body = dto.Body;

        if (dto.TemplateId.HasValue)
        {
            var template = await _templateRepository.GetByIdAsync(
                dto.TemplateId.Value, dto.OrganizationId, cancellationToken);
            if (template is null)
                throw new InvalidOperationException("Template not found.");
            body = template.Body;
        }

        if (string.IsNullOrWhiteSpace(body))
            throw new InvalidOperationException("Body or TemplateId is required.");

        var campaign = new Campaign
        {
            OrganizationId = dto.OrganizationId,
            CreatedByUserId = dto.CreatedByUserId,
            Title = dto.Title,
            Subject = dto.Subject,
            Message = body,
            Channel = dto.Channel,
            Status = CampaignStatus.Draft,
            ScheduledAt = dto.ScheduledAt,
        };

        await _campaignRepository.AddAsync(campaign, cancellationToken);
        await _campaignRepository.SaveChangesAsync(cancellationToken);

        // Notify org — fire and forget, never block create
        var creator = dto.CreatedByUserId.HasValue
            ? await _userRepository.GetByIdAsync(dto.CreatedByUserId.Value, cancellationToken)
            : null;

        var creatorName = creator?.FullName ?? "A team member";
        var creatorEmail = creator?.Email ?? "—";
        var preview = StripHtml(body);
        var scheduledNote = dto.ScheduledAt.HasValue
            ? $"<tr style='background:#f9f9f9'><td style='padding:8px;color:#888;width:140px'>Scheduled for</td><td style='padding:8px'>{dto.ScheduledAt.Value:dddd, MMMM d yyyy 'at' h:mm tt}</td></tr>"
            : "";

        _ = Task.Run(() => _orgNotifier.NotifyAsync(
            dto.OrganizationId,
            subject: $"New campaign created: {campaign.Title}",
            html: $"""
                <div style="font-family:sans-serif;max-width:600px;margin:0 auto">
                  <h2 style="color:#7c3aed">New Campaign Created </h2>
                  <p><strong>{creatorName}</strong> created a new campaign draft.</p>
                  <table style="width:100%;border-collapse:collapse;margin:20px 0">
                    <tr><td style="padding:8px;color:#888;width:140px">Campaign</td><td style="padding:8px;font-weight:600">{campaign.Title}</td></tr>
                    <tr style="background:#f9f9f9"><td style="padding:8px;color:#888">Subject</td><td style="padding:8px">{campaign.Subject}</td></tr>
                    <tr><td style="padding:8px;color:#888">Created by</td><td style="padding:8px">{creatorName} ({creatorEmail})</td></tr>
                    <tr style="background:#f9f9f9"><td style="padding:8px;color:#888">Creator ID</td><td style="padding:8px;font-family:monospace;font-size:12px">{dto.CreatedByUserId}</td></tr>
                    <tr><td style="padding:8px;color:#888">Status</td><td style="padding:8px">Draft</td></tr>
                    {scheduledNote}
                  </table>
                  <div style="background:#f5f5f5;border-left:4px solid #7c3aed;padding:16px;margin:20px 0;border-radius:4px">
                    <p style="margin:0 0 8px;color:#888;font-size:12px;text-transform:uppercase;letter-spacing:1px">Message Preview</p>
                    <p style="margin:0;color:#333;font-size:14px;line-height:1.6">{preview}</p>
                  </div>
                  <p style="color:#888;font-size:12px">You're receiving this because you're a member of this organization on NotificationHub.</p>
                </div>
                """,
            text: $"New campaign: {campaign.Title}\nCreated by: {creatorName} ({creatorEmail})\nSubject: {campaign.Subject}\n\nPreview:\n{preview}",
            cancellationToken: CancellationToken.None
        ));

        return campaign;
    }

    public async Task<AddRecipientsResult> AddRecipientsAsync(
        AddRecipientsDto dto, CancellationToken cancellationToken = default)
    {
        var campaign = await _campaignRepository.GetByIdAsync(
            dto.CampaignId, dto.OrganizationId, cancellationToken)
            ?? throw new InvalidOperationException("Campaign not found.");

        if (campaign.Status != CampaignStatus.Draft)
            throw new InvalidOperationException("Can only add recipients to a draft campaign.");

        var emails = dto.RawEmails
            .SelectMany(e => e.Split(new[] { ',', '\n', '\r', ';' }, StringSplitOptions.RemoveEmptyEntries))
            .Select(e => e.Trim().ToLowerInvariant())
            .Where(e => e.Contains('@'))
            .Distinct()
            .ToList();

        if (emails.Count == 0)
            throw new InvalidOperationException("No valid email addresses found.");

        var newRecipients = new List<CampaignRecipient>();
        foreach (var email in emails)
        {
            var exists = await _campaignRepository.RecipientExistsAsync(
                dto.CampaignId, email, cancellationToken);
            if (!exists)
            {
                newRecipients.Add(new CampaignRecipient
                {
                    CampaignId = dto.CampaignId,
                    OrganizationId = dto.OrganizationId,
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

        return new AddRecipientsResult(newRecipients.Count, emails.Count - newRecipients.Count);
    }

    public async Task SendAsync(
        Guid campaignId, Guid organizationId, Guid? triggeredByUserId,
        CancellationToken cancellationToken = default)
    {
        var campaign = await _campaignRepository.GetByIdAsync(campaignId, organizationId, cancellationToken)
            ?? throw new InvalidOperationException("Campaign not found.");

        if (campaign.Status != CampaignStatus.Draft && campaign.Status != CampaignStatus.Paused)
            throw new InvalidOperationException("Campaign must be Draft or Paused to send.");

        if (campaign.TotalRecipients == 0)
            throw new InvalidOperationException("Add recipients before sending.");

        campaign.Status = CampaignStatus.Running;
        campaign.StartedAt = _clock.UtcNow;
        await _campaignRepository.SaveChangesAsync(cancellationToken);

        var sender = triggeredByUserId.HasValue
            ? await _userRepository.GetByIdAsync(triggeredByUserId.Value, cancellationToken)
            : null;

        var senderName = sender?.FullName ?? "A team member";
        var senderEmail = sender?.Email ?? "—";
        var preview = StripHtml(campaign.Message);
        var startedTime = campaign.StartedAt?.ToString("h:mm tt, MMM d yyyy") ?? "now";

        _ = Task.Run(() => _orgNotifier.NotifyAsync(
            organizationId,
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
                    <tr><td style="padding:8px;color:#888">Sent by</td><td style="padding:8px">{senderName} ({senderEmail})</td></tr>
                    <tr style="background:#f9f9f9"><td style="padding:8px;color:#888">Sender ID</td><td style="padding:8px;font-family:monospace;font-size:12px">{triggeredByUserId}</td></tr>
                  </table>
                  <div style="background:#f5f5f5;border-left:4px solid #0d9488;padding:16px;margin:20px 0;border-radius:4px">
                    <p style="margin:0 0 8px;color:#888;font-size:12px;text-transform:uppercase;letter-spacing:1px">Message Preview</p>
                    <p style="margin:0;color:#333;font-size:14px;line-height:1.6">{preview}</p>
                  </div>
                  <p style="color:#888;font-size:12px">You're receiving this because you're a member of this organization on NotificationHub.</p>
                </div>
                """,
            text: $"Campaign sending: {campaign.Title}\nStarted: {startedTime}\nRecipients: {campaign.TotalRecipients}\nSent by: {senderName} ({senderEmail})\n\nPreview:\n{preview}",
            cancellationToken: CancellationToken.None
        ));
    }

    public async Task ScheduleAsync(
        Guid campaignId, Guid organizationId, DateTime scheduledAt,
        CancellationToken cancellationToken = default)
    {
        var campaign = await _campaignRepository.GetByIdAsync(campaignId, organizationId, cancellationToken)
            ?? throw new InvalidOperationException("Campaign not found.");

        if (campaign.Status != CampaignStatus.Draft)
            throw new InvalidOperationException("Only draft campaigns can be scheduled.");

        if (scheduledAt <= _clock.UtcNow)
            throw new InvalidOperationException("Scheduled time must be in the future.");

        if (campaign.TotalRecipients == 0)
            throw new InvalidOperationException("Add recipients before scheduling.");

        campaign.Status = CampaignStatus.Scheduled;
        campaign.ScheduledAt = scheduledAt;
        await _campaignRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task PauseAsync(
        Guid campaignId, Guid organizationId, CancellationToken cancellationToken = default)
    {
        var campaign = await _campaignRepository.GetByIdAsync(campaignId, organizationId, cancellationToken)
            ?? throw new InvalidOperationException("Campaign not found.");

        if (campaign.Status != CampaignStatus.Running)
            throw new InvalidOperationException("Only running campaigns can be paused.");

        campaign.Status = CampaignStatus.Paused;
        await _campaignRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task ResumeAsync(
        Guid campaignId, Guid organizationId, CancellationToken cancellationToken = default)
    {
        var campaign = await _campaignRepository.GetByIdAsync(campaignId, organizationId, cancellationToken)
            ?? throw new InvalidOperationException("Campaign not found.");

        if (campaign.Status != CampaignStatus.Paused)
            throw new InvalidOperationException("Only paused campaigns can be resumed.");

        campaign.Status = CampaignStatus.Running;
        campaign.StartedAt ??= _clock.UtcNow;
        await _campaignRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(
        Guid campaignId, Guid organizationId, CancellationToken cancellationToken = default)
    {
        var campaign = await _campaignRepository.GetByIdAsync(campaignId, organizationId, cancellationToken)
            ?? throw new InvalidOperationException("Campaign not found.");

        if (campaign.Status == CampaignStatus.Running)
            throw new InvalidOperationException("Pause the campaign before deleting.");

        campaign.DeletedAt = _clock.UtcNow;
        await _campaignRepository.SaveChangesAsync(cancellationToken);
    }

    private static string StripHtml(string html)
    {
        var stripped = System.Text.RegularExpressions.Regex.Replace(html ?? "", "<[^>]+>", "");
        return stripped.Length > 300 ? stripped[..300] + "..." : stripped;
    }
}