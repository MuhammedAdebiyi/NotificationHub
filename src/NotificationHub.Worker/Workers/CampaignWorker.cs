using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NotificationHub.Application.Abstractions;
using NotificationHub.Domain.Entities;
using NotificationHub.Domain.Enums;
using System.Text.Json;

namespace NotificationHub.Worker.Workers;

public class CampaignWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CampaignWorker> _logger;

    public CampaignWorker(IServiceScopeFactory scopeFactory, ILogger<CampaignWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("CampaignWorker started");
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessScheduledCampaignsAsync(stoppingToken);
                await ProcessRunningCampaignsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CampaignWorker loop");
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }

    private static string StripHtml(string html)
    {
        var stripped = System.Text.RegularExpressions.Regex.Replace(html ?? "", "<[^>]+>", "");
        return stripped.Length > 300 ? stripped[..300] + "..." : stripped;
    }

    private async Task SendOrgEmailsAsync(
        IOrganizationMemberRepository memberRepository,
        IEmailProvider emailProvider,
        Guid orgId,
        string subject,
        string html,
        string text,
        CancellationToken stoppingToken)
    {
        var members = await memberRepository.GetByOrgAsync(orgId, stoppingToken);
        foreach (var member in members.Where(m => m.Role != "revoked" && m.User?.Email != null))
        {
            try
            {
                await emailProvider.SendAsync(new EmailMessage(
                    From: "NotificationHub <noreply@coursevaultai.app>",
                    To: member.User!.Email,
                    Subject: subject,
                    Html: html,
                    Text: text
                ), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send org notification to {Email}", member.User!.Email);
            }
        }
    }

    private async Task ProcessScheduledCampaignsAsync(CancellationToken stoppingToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var campaignRepository = scope.ServiceProvider.GetRequiredService<ICampaignRepository>();
        var memberRepository = scope.ServiceProvider.GetRequiredService<IOrganizationMemberRepository>();
        var emailProvider = scope.ServiceProvider.GetRequiredService<IEmailProvider>();
        var clock = scope.ServiceProvider.GetRequiredService<NotificationHub.Shared.Abstractions.IClock>();

        var ready = await campaignRepository.GetScheduledReadyAsync(clock.UtcNow, stoppingToken);

        foreach (var campaign in ready)
        {
            campaign.Status = CampaignStatus.Running;
            campaign.StartedAt = clock.UtcNow;
            _logger.LogInformation("Campaign {Id} — {Title} — scheduled time reached, starting", campaign.Id, campaign.Title);

            var members = await memberRepository.GetByOrgAsync(campaign.OrganizationId, stoppingToken);
            var creator = members.FirstOrDefault(m => m.UserId == campaign.CreatedByUserId);
            var creatorName = creator?.User?.FullName ?? "Someone";
            var creatorEmail = creator?.User?.Email ?? "—";
            var creatorId = campaign.CreatedByUserId?.ToString() ?? "—";
            var scheduledTime = campaign.ScheduledAt?.ToString("dddd, MMMM d yyyy 'at' h:mm tt") ?? "now";
            var startedTime = campaign.StartedAt?.ToString("h:mm tt, MMM d yyyy") ?? "now";
            var bodyPreview = StripHtml(campaign.Message);

            await SendOrgEmailsAsync(
                memberRepository, emailProvider, campaign.OrganizationId,
                subject: $"Campaign started: {campaign.Title}",
                html: $"""
                    <div style="font-family:sans-serif;max-width:600px;margin:0 auto">
                      <h2 style="color:#7c3aed">Campaign Started </h2>
                      <p>The scheduled campaign <strong>{campaign.Title}</strong> has started sending.</p>
                      <table style="width:100%;border-collapse:collapse;margin:20px 0">
                        <tr><td style="padding:8px;color:#888;width:140px">Campaign</td><td style="padding:8px;font-weight:600">{campaign.Title}</td></tr>
                        <tr style="background:#f9f9f9"><td style="padding:8px;color:#888">Subject</td><td style="padding:8px">{campaign.Subject}</td></tr>
                        <tr><td style="padding:8px;color:#888">Scheduled for</td><td style="padding:8px">{scheduledTime}</td></tr>
                        <tr style="background:#f9f9f9"><td style="padding:8px;color:#888">Started at</td><td style="padding:8px">{startedTime}</td></tr>
                        <tr><td style="padding:8px;color:#888">Recipients</td><td style="padding:8px">{campaign.TotalRecipients}</td></tr>
                        <tr style="background:#f9f9f9"><td style="padding:8px;color:#888">Created by</td><td style="padding:8px">{creatorName} ({creatorEmail})</td></tr>
                        <tr><td style="padding:8px;color:#888">Creator ID</td><td style="padding:8px;font-family:monospace;font-size:12px">{creatorId}</td></tr>
                      </table>
                      <div style="background:#f5f5f5;border-left:4px solid #7c3aed;padding:16px;margin:20px 0;border-radius:4px">
                        <p style="margin:0 0 8px;color:#888;font-size:12px;text-transform:uppercase;letter-spacing:1px">Message Preview</p>
                        <p style="margin:0;color:#333;font-size:14px;line-height:1.6">{bodyPreview}</p>
                      </div>
                      <p style="color:#888;font-size:12px">You're receiving this because you're a member of this organization on NotificationHub.</p>
                    </div>
                    """,
                text: $"Campaign started: {campaign.Title}\nScheduled: {scheduledTime}\nStarted: {startedTime}\nRecipients: {campaign.TotalRecipients}\nCreated by: {creatorName} ({creatorEmail})\n\nPreview:\n{bodyPreview}",
                stoppingToken
            );
        }

        if (ready.Count > 0)
            await campaignRepository.SaveChangesAsync(stoppingToken);
    }

    private async Task ProcessRunningCampaignsAsync(CancellationToken stoppingToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var campaignRepository = scope.ServiceProvider.GetRequiredService<ICampaignRepository>();
        var notificationRepository = scope.ServiceProvider.GetRequiredService<INotificationRepository>();
        var memberRepository = scope.ServiceProvider.GetRequiredService<IOrganizationMemberRepository>();
        var emailProvider = scope.ServiceProvider.GetRequiredService<IEmailProvider>();
        var queue = scope.ServiceProvider.GetRequiredService<INotificationQueue>();
        var clock = scope.ServiceProvider.GetRequiredService<NotificationHub.Shared.Abstractions.IClock>();

        var running = await campaignRepository.GetRunningAsync(stoppingToken);

        foreach (var campaign in running)
        {
            _logger.LogInformation("Processing campaign {Id} — {Title}", campaign.Id, campaign.Title);

            var batch = await campaignRepository.GetUnprocessedRecipientsAsync(
                campaign.Id, 100, stoppingToken);

            if (batch.Count == 0)
            {
                campaign.Status = CampaignStatus.Completed;
                campaign.CompletedAt = clock.UtcNow;

                string durationText = "—";
                if (campaign.StartedAt.HasValue)
                {
                    var secs = (int)(campaign.CompletedAt.Value - campaign.StartedAt.Value).TotalSeconds;
                    durationText = secs < 60 ? $"{secs} seconds" : $"{secs / 60}m {secs % 60}s";
                    _logger.LogInformation("Campaign {Id} completed in {Duration}", campaign.Id, durationText);
                }
                else
                {
                    _logger.LogInformation("Campaign {Id} completed", campaign.Id);
                }

                await campaignRepository.SaveChangesAsync(stoppingToken);

                var completedTime = campaign.CompletedAt?.ToString("h:mm tt, MMM d yyyy") ?? "now";
                var startedTime = campaign.StartedAt?.ToString("h:mm tt, MMM d yyyy") ?? "—";
                var members = await memberRepository.GetByOrgAsync(campaign.OrganizationId, stoppingToken);
                var creator = members.FirstOrDefault(m => m.UserId == campaign.CreatedByUserId);
                var creatorName = creator?.User?.FullName ?? "Someone";
                var creatorEmail = creator?.User?.Email ?? "—";

                await SendOrgEmailsAsync(
                    memberRepository, emailProvider, campaign.OrganizationId,
                    subject: $"Campaign completed: {campaign.Title}",
                    html: $"""
                        <div style="font-family:sans-serif;max-width:600px;margin:0 auto">
                          <h2 style="color:#0d9488">Campaign Completed ✓</h2>
                          <p>The campaign <strong>{campaign.Title}</strong> finished sending.</p>
                          <table style="width:100%;border-collapse:collapse;margin:20px 0">
                            <tr><td style="padding:8px;color:#888;width:140px">Campaign</td><td style="padding:8px;font-weight:600">{campaign.Title}</td></tr>
                            <tr style="background:#f9f9f9"><td style="padding:8px;color:#888">Subject</td><td style="padding:8px">{campaign.Subject}</td></tr>
                            <tr><td style="padding:8px;color:#888">Started at</td><td style="padding:8px">{startedTime}</td></tr>
                            <tr style="background:#f9f9f9"><td style="padding:8px;color:#888">Completed at</td><td style="padding:8px">{completedTime}</td></tr>
                            <tr><td style="padding:8px;color:#888">Duration</td><td style="padding:8px;font-weight:600;color:#0d9488">{durationText}</td></tr>
                            <tr style="background:#f9f9f9"><td style="padding:8px;color:#888">Total sent</td><td style="padding:8px">{campaign.TotalRecipients}</td></tr>
                            <tr><td style="padding:8px;color:#888">Created by</td><td style="padding:8px">{creatorName} ({creatorEmail})</td></tr>
                          </table>
                          <p style="color:#888;font-size:12px">You're receiving this because you're a member of this organization on NotificationHub.</p>
                        </div>
                        """,
                    text: $"Campaign completed: {campaign.Title}\nStarted: {startedTime}\nCompleted: {completedTime}\nDuration: {durationText}\nTotal sent: {campaign.TotalRecipients}\nCreated by: {creatorName} ({creatorEmail})",
                    stoppingToken
                );

                continue;
            }

            foreach (var recipient in batch)
            {
                var notification = new Notification
                {
                    OrganizationId = campaign.OrganizationId,
                    RecipientEmail = recipient.RecipientEmail,
                    Type = $"Campaign_{campaign.Title}",
                    Channel = campaign.Channel,
                    Payload = JsonSerializer.Serialize(new
                    {
                        subject = campaign.Subject,
                        body = campaign.Message,
                    }),
                };

                await notificationRepository.AddAsync(notification, stoppingToken);
                await notificationRepository.SaveChangesAsync(stoppingToken);

                recipient.NotificationId = notification.Id;
                await queue.EnqueueAsync(notification.Id, stoppingToken);

                _logger.LogInformation("Queued notification for {Email} in campaign {CampaignId}",
                    recipient.RecipientEmail, campaign.Id);
            }

            await campaignRepository.SaveChangesAsync(stoppingToken);
        }
    }
}