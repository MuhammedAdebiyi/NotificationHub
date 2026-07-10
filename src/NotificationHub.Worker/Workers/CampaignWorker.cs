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

        // Wait 10s on startup — lets Neon wake up before first query
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

    private async Task ProcessScheduledCampaignsAsync(CancellationToken stoppingToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var campaignRepository = scope.ServiceProvider.GetRequiredService<ICampaignRepository>();
        var clock = scope.ServiceProvider.GetRequiredService<NotificationHub.Shared.Abstractions.IClock>();

        var ready = await campaignRepository.GetScheduledReadyAsync(clock.UtcNow, stoppingToken);
        foreach (var campaign in ready)
        {
            campaign.Status = CampaignStatus.Running;
            _logger.LogInformation("Campaign {Id} scheduled time reached — starting", campaign.Id);
        }

        if (ready.Count > 0)
            await campaignRepository.SaveChangesAsync(stoppingToken);
    }

    private async Task ProcessRunningCampaignsAsync(CancellationToken stoppingToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var campaignRepository = scope.ServiceProvider.GetRequiredService<ICampaignRepository>();
        var notificationRepository = scope.ServiceProvider.GetRequiredService<INotificationRepository>();
        var queue = scope.ServiceProvider.GetRequiredService<INotificationQueue>();

        var running = await campaignRepository.GetRunningAsync(stoppingToken);

        foreach (var campaign in running)
        {
            _logger.LogInformation("Processing campaign {Id} — {Title}", campaign.Id, campaign.Title);

            var batch = await campaignRepository.GetUnprocessedRecipientsAsync(
                campaign.Id, 100, stoppingToken);

            if (batch.Count == 0)
            {
                campaign.Status = CampaignStatus.Completed;
                _logger.LogInformation("Campaign {Id} completed", campaign.Id);
                await campaignRepository.SaveChangesAsync(stoppingToken);
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