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

    private static string BuildPayload(string subject, string body) =>
        JsonSerializer.Serialize(new { subject, body });

    private async Task QueueOrgNotificationsAsync(
        IOrganizationMemberRepository memberRepository,
        INotificationRepository notificationRepository,
        INotificationQueue queue,
        Guid orgId,
        string subject,
        string htmlBody,
        CancellationToken stoppingToken)
    {
        var members = await memberRepository.GetByOrgAsync(orgId, stoppingToken);
        foreach (var member in members.Where(m => m.Role != "revoked" && m.User?.Email != null))
        {
            var notification = new Notification
            {
                OrganizationId = orgId,
                RecipientEmail = member.User!.Email,
                Type = "OrgAlert",
                Channel = NotificationChannel.Email,
                Payload = BuildPayload(subject, htmlBody),
            };
            await notificationRepository.AddAsync(notification, stoppingToken);
            await notificationRepository.SaveChangesAsync(stoppingToken);
            await queue.EnqueueAsync(notification.Id, stoppingToken);
        }
    }

    private async Task ProcessScheduledCampaignsAsync(CancellationToken stoppingToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var campaignRepository = scope.ServiceProvider.GetRequiredService<ICampaignRepository>();
        var memberRepository = scope.ServiceProvider.GetRequiredService<IOrganizationMemberRepository>();
        var notificationRepository = scope.ServiceProvider.GetRequiredService<INotificationRepository>();
        var queue = scope.ServiceProvider.GetRequiredService<INotificationQueue>();
        var clock = scope.ServiceProvider.GetRequiredService<NotificationHub.Shared.Abstractions.IClock>();

        var ready = await campaignRepository.GetScheduledReadyAsync(clock.UtcNow, stoppingToken);

        foreach (var campaign in ready)
        {
            campaign.Status = CampaignStatus.Running;
            campaign.StartedAt = clock.UtcNow;
            _logger.LogInformation("Campaign {Id} — {Title} — scheduled time reached, starting",
                campaign.Id, campaign.Title);

            var members = await memberRepository.GetByOrgAsync(campaign.OrganizationId, stoppingToken);
            var creator = members.FirstOrDefault(m => m.UserId == campaign.CreatedByUserId);
            var creatorName = creator?.User?.FullName ?? "Someone";
            var creatorEmail = creator?.User?.Email ?? "—";
            var scheduledTime = campaign.ScheduledAt?.ToString("dddd, MMMM d yyyy 'at' h:mm tt") ?? "now";
            var startedTime = campaign.StartedAt?.ToString("h:mm tt, MMM d yyyy") ?? "now";
            var bodyPreview = StripHtml(campaign.Message);

            await QueueOrgNotificationsAsync(
                memberRepository, notificationRepository, queue,
                campaign.OrganizationId,
                subject: $"Campaign started: {campaign.Title}",
                htmlBody: BuildScheduledStartedEmail(
                    campaign.Title, campaign.Subject, scheduledTime,
                    startedTime, campaign.TotalRecipients,
                    creatorName, creatorEmail, bodyPreview),
                stoppingToken);

            campaign.StartNotificationSentAt = clock.UtcNow;
        }

        if (ready.Count > 0)
        {
            try
            {
                await campaignRepository.SaveChangesAsync(stoppingToken);
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException)
            {
                _logger.LogWarning(
                    "One or more scheduled campaigns were claimed by another worker.");
            }
        }
    }

    private async Task ProcessRunningCampaignsAsync(CancellationToken stoppingToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var campaignRepository = scope.ServiceProvider.GetRequiredService<ICampaignRepository>();
        var notificationRepository = scope.ServiceProvider.GetRequiredService<INotificationRepository>();
        var memberRepository = scope.ServiceProvider.GetRequiredService<IOrganizationMemberRepository>();
        var queue = scope.ServiceProvider.GetRequiredService<INotificationQueue>();
        var clock = scope.ServiceProvider.GetRequiredService<NotificationHub.Shared.Abstractions.IClock>();

        var running = await campaignRepository.GetRunningAsync(stoppingToken);

        foreach (var campaign in running)
        {
            _logger.LogInformation("Processing campaign {Id} — {Title}", campaign.Id, campaign.Title);

            // Send "campaign sending now" email on first worker pick-up only
            if (campaign.StartNotificationSentAt is null)
            {
                var members = await memberRepository.GetByOrgAsync(campaign.OrganizationId, stoppingToken);
                var creator = members.FirstOrDefault(m => m.UserId == campaign.CreatedByUserId);
                var creatorName = creator?.User?.FullName ?? "Someone";
                var creatorEmail = creator?.User?.Email ?? "—";
                var startedTime = campaign.StartedAt?.ToString("h:mm tt, MMM d yyyy") ?? "now";
                var bodyPreview = StripHtml(campaign.Message);

                await QueueOrgNotificationsAsync(
                    memberRepository, notificationRepository, queue,
                    campaign.OrganizationId,
                    subject: $"Campaign sending now: {campaign.Title}",
                    htmlBody: BuildSendingNowEmail(
                        campaign.Title, campaign.Subject,
                        creatorName, creatorEmail,
                        campaign.CreatedByUserId?.ToString() ?? "—",
                        campaign.TotalRecipients, startedTime, bodyPreview),
                    stoppingToken);

                campaign.StartNotificationSentAt = clock.UtcNow;

                try
                {
                    await campaignRepository.SaveChangesAsync(stoppingToken);
                }
                catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException)
                {
                    _logger.LogWarning(
                        "Campaign {CampaignId} was modified by another worker while marking StartNotificationSentAt.",
                        campaign.Id);

                    continue;
                }
            }

            var batch = await campaignRepository.GetUnprocessedRecipientsAsync(
                campaign.Id, 100, stoppingToken);

            if (batch.Count == 0)
            {
                campaign.Status = CampaignStatus.Completed;
                campaign.CompletedAt = clock.UtcNow;

                var secs = campaign.StartedAt.HasValue
                    ? (int)(campaign.CompletedAt.Value - campaign.StartedAt.Value).TotalSeconds
                    : 0;
                var durationText = secs < 60 ? $"{secs}s" : $"{secs / 60}m {secs % 60}s";

                _logger.LogInformation("Campaign {Id} completed in {Duration}", campaign.Id, durationText);
                try
                {
                    await campaignRepository.SaveChangesAsync(stoppingToken);
                }
                catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException)
                {
                    _logger.LogWarning(
                        "Campaign {CampaignId} was modified while processing recipients.",
                        campaign.Id);

                    continue;
                }

                var allMembers = await memberRepository.GetByOrgAsync(campaign.OrganizationId, stoppingToken);
                var completionCreator = allMembers.FirstOrDefault(m => m.UserId == campaign.CreatedByUserId);
                var completedTime = campaign.CompletedAt?.ToString("h:mm tt, MMM d yyyy") ?? "now";
                var startedTime = campaign.StartedAt?.ToString("h:mm tt, MMM d yyyy") ?? "—";

                await QueueOrgNotificationsAsync(
                    memberRepository, notificationRepository, queue,
                    campaign.OrganizationId,
                    subject: $"Campaign completed: {campaign.Title}",
                    htmlBody: BuildCompletedEmail(
                        campaign.Title, campaign.Subject, startedTime,
                        completedTime, durationText, campaign.TotalRecipients,
                        completionCreator?.User?.FullName ?? "Someone",
                        completionCreator?.User?.Email ?? "—"),
                    stoppingToken);

                continue;
            }

            // Batch save — one SaveChanges for all notifications, not one per recipient
            var notifications = new List<Notification>();
            foreach (var recipient in batch)
            {
                var notification = new Notification
                {
                    OrganizationId = campaign.OrganizationId,
                    RecipientEmail = recipient.RecipientEmail,
                    CampaignId = campaign.Id,
                    Type = $"Campaign_{campaign.Title}",
                    Channel = campaign.Channel,
                    Payload = JsonSerializer.Serialize(new
                    {
                        subject = campaign.Subject,
                        body = campaign.Message,
                    }),
                };
                notifications.Add(notification);
                await notificationRepository.AddAsync(notification, stoppingToken);
            }

            // Single save for the whole batch
            await notificationRepository.SaveChangesAsync(stoppingToken);

            // Enqueue all and link recipients
            for (int i = 0; i < batch.Count; i++)
            {
                batch[i].NotificationId = notifications[i].Id;
                await queue.EnqueueAsync(notifications[i].Id, stoppingToken);
                _logger.LogInformation("Queued notification for {Email} in campaign {CampaignId}",
                    batch[i].RecipientEmail, campaign.Id);
            }

            await campaignRepository.SaveChangesAsync(stoppingToken);
        }
    }

    private static string BuildScheduledStartedEmail(
        string title, string subject, string scheduledTime,
        string startedTime, int recipients, string creatorName,
        string creatorEmail, string bodyPreview) => $"""
        <div style="font-family:sans-serif;max-width:600px;margin:0 auto;color:#1a1a2e">
          <div style="background:#7c3aed;padding:32px;border-radius:12px 12px 0 0">
            <h1 style="margin:0;color:white;font-size:22px;font-weight:700">Campaign Started </h1>
            <p style="margin:8px 0 0;color:rgba(255,255,255,0.8);font-size:14px">Your scheduled campaign is now sending</p>
          </div>
          <div style="background:#fafafa;border:1px solid #eee;border-top:none;border-radius:0 0 12px 12px;padding:32px">
            <table style="width:100%;border-collapse:collapse;margin-bottom:24px">
              <tr><td style="padding:10px 12px;color:#888;font-size:13px;width:140px;border-bottom:1px solid #f0f0f0">Campaign</td><td style="padding:10px 12px;font-weight:600;border-bottom:1px solid #f0f0f0">{title}</td></tr>
              <tr><td style="padding:10px 12px;color:#888;font-size:13px;border-bottom:1px solid #f0f0f0;background:#fff">Subject</td><td style="padding:10px 12px;border-bottom:1px solid #f0f0f0;background:#fff">{subject}</td></tr>
              <tr><td style="padding:10px 12px;color:#888;font-size:13px;border-bottom:1px solid #f0f0f0">Scheduled for</td><td style="padding:10px 12px;border-bottom:1px solid #f0f0f0">{scheduledTime}</td></tr>
              <tr><td style="padding:10px 12px;color:#888;font-size:13px;border-bottom:1px solid #f0f0f0;background:#fff">Started at</td><td style="padding:10px 12px;font-weight:600;color:#7c3aed;border-bottom:1px solid #f0f0f0;background:#fff">{startedTime}</td></tr>
              <tr><td style="padding:10px 12px;color:#888;font-size:13px;border-bottom:1px solid #f0f0f0">Recipients</td><td style="padding:10px 12px;border-bottom:1px solid #f0f0f0">{recipients:N0}</td></tr>
              <tr><td style="padding:10px 12px;color:#888;font-size:13px;background:#fff">Created by</td><td style="padding:10px 12px;background:#fff">{creatorName} <span style="color:#888;font-size:12px">({creatorEmail})</span></td></tr>
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

    private static string BuildCompletedEmail(
        string title, string subject, string startedTime,
        string completedTime, string duration, int totalSent,
        string creatorName, string creatorEmail) => $"""
        <div style="font-family:sans-serif;max-width:600px;margin:0 auto;color:#1a1a2e">
          <div style="background:#0d9488;padding:32px;border-radius:12px 12px 0 0">
            <h1 style="margin:0;color:white;font-size:22px;font-weight:700"> Campaign Completed</h1>
            <p style="margin:8px 0 0;color:rgba(255,255,255,0.8);font-size:14px">All emails have been delivered</p>
          </div>
          <div style="background:#fafafa;border:1px solid #eee;border-top:none;border-radius:0 0 12px 12px;padding:32px">
            <table style="width:100%;border-collapse:collapse;margin-bottom:24px">
              <tr><td style="padding:10px 12px;color:#888;font-size:13px;width:140px;border-bottom:1px solid #f0f0f0">Campaign</td><td style="padding:10px 12px;font-weight:600;border-bottom:1px solid #f0f0f0">{title}</td></tr>
              <tr><td style="padding:10px 12px;color:#888;font-size:13px;border-bottom:1px solid #f0f0f0;background:#fff">Subject</td><td style="padding:10px 12px;border-bottom:1px solid #f0f0f0;background:#fff">{subject}</td></tr>
              <tr><td style="padding:10px 12px;color:#888;font-size:13px;border-bottom:1px solid #f0f0f0">Started at</td><td style="padding:10px 12px;border-bottom:1px solid #f0f0f0">{startedTime}</td></tr>
              <tr><td style="padding:10px 12px;color:#888;font-size:13px;border-bottom:1px solid #f0f0f0;background:#fff">Completed at</td><td style="padding:10px 12px;border-bottom:1px solid #f0f0f0;background:#fff">{completedTime}</td></tr>
              <tr><td style="padding:10px 12px;color:#888;font-size:13px;border-bottom:1px solid #f0f0f0">Duration</td><td style="padding:10px 12px;font-weight:700;color:#0d9488;font-size:16px;border-bottom:1px solid #f0f0f0">{duration}</td></tr>
              <tr><td style="padding:10px 12px;color:#888;font-size:13px;border-bottom:1px solid #f0f0f0;background:#fff">Total sent</td><td style="padding:10px 12px;font-weight:600;border-bottom:1px solid #f0f0f0;background:#fff">{totalSent:N0} recipients</td></tr>
              <tr><td style="padding:10px 12px;color:#888;font-size:13px">Created by</td><td style="padding:10px 12px">{creatorName} <span style="color:#888;font-size:12px">({creatorEmail})</span></td></tr>
            </table>
            <p style="color:#bbb;font-size:11px;margin-top:24px;text-align:center">You're receiving this because you're a member of this organization on NotificationHub.</p>
          </div>
        </div>
        """;
}