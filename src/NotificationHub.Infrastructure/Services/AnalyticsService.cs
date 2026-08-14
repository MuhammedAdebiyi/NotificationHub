using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NotificationHub.Application.Abstractions;
using NotificationHub.Application.Features.Analytics.DTOs;
using NotificationHub.Domain.Enums;
using NotificationHub.Infrastructure.Persistence;
using StackExchange.Redis;
using System.Diagnostics;

namespace NotificationHub.Infrastructure.Services;

public sealed class AnalyticsService : IAnalyticsService
{
    private readonly AppDbContext _context;
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<AnalyticsService> _logger;
    private static readonly DateTime _startedAt = DateTime.UtcNow;

    public AnalyticsService(
        AppDbContext context,
        IConnectionMultiplexer redis,
        ILogger<AnalyticsService> logger)
    {
        _context = context;
        _redis   = redis;
        _logger  = logger;
    }

    // ─── Health ──────────────────────────────────────────────────────────────

    public async Task<AnalyticsHealthDto> GetHealthAsync(Guid organizationId, CancellationToken ct = default)
    {
        var todayUtc = DateTime.UtcNow.Date;
        var db       = _redis.GetDatabase();

        var sent   = await _context.Notifications.CountAsync(n => n.OrganizationId == organizationId && n.Status == NotificationStatus.Sent   && n.CreatedAt >= todayUtc, ct);
        var failed = await _context.Notifications.CountAsync(n => n.OrganizationId == organizationId && n.Status == NotificationStatus.Failed  && n.CreatedAt >= todayUtc, ct);
        var total  = sent + failed;
        var successRate = total == 0 ? 100.0 : Math.Round((double)sent / total * 100, 2);

        var server        = _redis.GetServer(_redis.GetEndPoints()[0]);
        var heartbeats    = server.Keys(pattern: "worker:heartbeat:*").ToArray();
        var workersOnline = heartbeats.Length;

        var dbLatency    = await PingDatabaseAsync(ct);
        var redisLatency = await PingRedisAsync();

        var queueLength    = await db.ListLengthAsync("notification_queue");
        var queueLatencyMs = queueLength * 2;

        var deadLetters = await _context.Notifications.CountAsync(
            n => n.OrganizationId == organizationId &&
                 n.Status == NotificationStatus.DeadLetter &&
                 n.CreatedAt >= todayUtc, ct);

        var overallStatus = successRate < 90 || workersOnline == 0 ? "critical"
                          : successRate < 98 || deadLetters > 10 || queueLatencyMs > 2000 ? "warning"
                          : "healthy";

        // FIX: use the IsSuccess flag the worker already sets, instead of string-matching
        // the word "success" inside the raw provider response text (the provider's actual
        // response format, e.g. "accepted: id=...", never contains that word).
        var recentLogs = await _context.NotificationLogs
            .Where(l => _context.Notifications
                .Where(n => n.OrganizationId == organizationId)
                .Select(n => n.Id)
                .Contains(l.NotificationId))
            .OrderByDescending(l => l.CreatedAt)
            .Take(100)
            .Select(l => l.IsSuccess)
            .ToListAsync(ct);

        var providerSuccessRate = recentLogs.Count == 0 ? 100.0
            : Math.Round((double)recentLogs.Count(s => s) / recentLogs.Count * 100, 1);

        var components = new List<ComponentHealthDto>
        {
            new("Database", dbLatency < 100 ? "healthy" : "degraded",           LatencyMs: dbLatency,    Workers: null,         SuccessRate: null),
            new("Redis",    redisLatency < 50 ? "healthy" : "degraded",         LatencyMs: redisLatency, Workers: null,         SuccessRate: null),
            new("Worker",   workersOnline > 0 ? "healthy" : "down",             LatencyMs: null,         Workers: workersOnline, SuccessRate: null),
            new("Resend", providerSuccessRate >= 98 ? "healthy" : "degraded", LatencyMs: null,         Workers: null,         SuccessRate: providerSuccessRate),
        };

        return new AnalyticsHealthDto(overallStatus, successRate, queueLatencyMs, workersOnline, null, components);
    }

    // ─── Overview ────────────────────────────────────────────────────────────

    public async Task<AnalyticsOverviewDto> GetOverviewAsync(Guid organizationId, CancellationToken ct = default)
    {
        var todayUtc     = DateTime.UtcNow.Date;
        var yesterdayUtc = todayUtc.AddDays(-1);

        var stats = await _context.Notifications
            .Where(n => n.OrganizationId == organizationId && n.CreatedAt >= todayUtc)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Sent       = g.Count(n => n.Status == NotificationStatus.Sent),
                Failed     = g.Count(n => n.Status == NotificationStatus.Failed),
                DeadLetter = g.Count(n => n.Status == NotificationStatus.DeadLetter),
                Pending    = g.Count(n => n.Status == NotificationStatus.Pending),
                Retrying   = g.Count(n => n.Status == NotificationStatus.Retrying),
            })
            .FirstOrDefaultAsync(ct);

        var sent        = stats?.Sent       ?? 0;
        var failed      = stats?.Failed     ?? 0;
        var deadLetter  = stats?.DeadLetter ?? 0;
        var totalToday  = sent + failed;
        var successRate = totalToday == 0 ? 100.0 : Math.Round((double)sent / totalToday * 100, 2);

        var yesterdayStats = await _context.Notifications
            .Where(n => n.OrganizationId == organizationId &&
                        n.CreatedAt >= yesterdayUtc && n.CreatedAt < todayUtc)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Sent   = g.Count(n => n.Status == NotificationStatus.Sent),
                Failed = g.Count(n => n.Status == NotificationStatus.Failed),
            })
            .FirstOrDefaultAsync(ct);

        var yesterdaySent        = yesterdayStats?.Sent   ?? 0;
        var yesterdayFailed      = yesterdayStats?.Failed ?? 0;
        var yesterdayTotal       = yesterdaySent + yesterdayFailed;
        var yesterdaySuccessRate = yesterdayTotal == 0 ? 100.0 : Math.Round((double)yesterdaySent / yesterdayTotal * 100, 2);

        var sentDelta        = yesterdaySent == 0 ? 0 : Math.Round((double)(sent - yesterdaySent) / yesterdaySent * 100, 1);
        var successRateDelta = Math.Round(successRate - yesterdaySuccessRate, 2);

        var campaignsRunning   = await _context.Campaigns.CountAsync(c => c.OrganizationId == organizationId && c.Status == CampaignStatus.Running,   ct);
        var campaignsScheduled = await _context.Campaigns.CountAsync(c => c.OrganizationId == organizationId && c.Status == CampaignStatus.Scheduled, ct);

        var db          = _redis.GetDatabase();
        var queueLength = (int)await db.ListLengthAsync("notification_queue");

        var server        = _redis.GetServer(_redis.GetEndPoints()[0]);
        var workersOnline = server.Keys(pattern: "worker:heartbeat:*").Count();

        var dlqNeedingReview = await _context.Notifications
            .CountAsync(n => n.OrganizationId == organizationId &&
                             n.Status == NotificationStatus.DeadLetter &&
                             n.CreatedAt >= todayUtc, ct);

        var throughput            = workersOnline > 0 ? workersOnline * 50 : 1;
        var estimatedDrainMinutes = Math.Round((double)queueLength / throughput, 1);

        var oldestPending = await _context.Notifications
            .Where(n => n.OrganizationId == organizationId && n.Status == NotificationStatus.Pending)
            .OrderBy(n => n.CreatedAt)
            .Select(n => (DateTime?)n.CreatedAt)
            .FirstOrDefaultAsync(ct);

        var oldestPendingSeconds = oldestPending.HasValue
            ? (long)(DateTime.UtcNow - oldestPending.Value).TotalSeconds
            : 0L;

        return new AnalyticsOverviewDto(
            NotificationsSent:          sent,
            NotificationsSentDelta:     sentDelta,
            SuccessRate:                successRate,
            SuccessRateDelta:           successRateDelta,
            QueueDepth:                 queueLength,
            WorkersActive:              workersOnline,
            AvgSendTimeMs:              420,
            P95SendTimeMs:              1200,
            DeadLetters:                deadLetter,
            DeadLettersNeedingReview:   dlqNeedingReview,
            CampaignsRunning:           campaignsRunning,
            CampaignsScheduled:         campaignsScheduled,
            ApiCallsToday:              0,
            Plan:                       "Free",
            PlanUsagePct:               Math.Round((double)sent / 100_000 * 100, 1),
            EstimatedQueueDrainMinutes: estimatedDrainMinutes,
            OldestPendingSeconds:       oldestPendingSeconds
        );
    }

    // ─── Timeline ────────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<TimelinePointDto>> GetTimelineAsync(Guid organizationId, CancellationToken ct = default)
    {
        var now   = DateTime.UtcNow;
        var since = now.AddHours(-23); // 23 hours ago through the CURRENT (partial) hour = 24 buckets

        var raw = await _context.Notifications
            .Where(n => n.OrganizationId == organizationId && n.CreatedAt >= since)
            .GroupBy(n => new { n.CreatedAt.Date, Hour = n.CreatedAt.Hour })
            .Select(g => new
            {
                g.Key.Date,
                g.Key.Hour,
                Sent       = g.Count(n => n.Status == NotificationStatus.Sent),
                Failed     = g.Count(n => n.Status == NotificationStatus.Failed),
                Retrying   = g.Count(n => n.Status == NotificationStatus.Retrying),
                Processing = g.Count(n => n.Status == NotificationStatus.Processing),
                Queued     = g.Count(n => n.Status == NotificationStatus.Pending),
            })
            .OrderBy(g => g.Date).ThenBy(g => g.Hour)
            .ToListAsync(ct);

        var buckets = Enumerable.Range(0, 24).Select(i =>
        {
            // i=0 -> since (now-23h) ... i=23 -> now. Includes the current, still-filling hour,
            // which the old (0..23 from since) version silently dropped.
            var slot  = DateTime.SpecifyKind(since.AddHours(i), DateTimeKind.Utc);
            var found = raw.FirstOrDefault(r => r.Date == slot.Date && r.Hour == slot.Hour);
            return new TimelinePointDto(
                // ISO 8601 UTC timestamp — NOT a pre-formatted clock string. The frontend
                // converts to the viewer's local timezone at render time. Sending a
                // formatted "HH:00" string here bakes in server/UTC time, which was wrong
                // for any org member outside UTC.
                Hour:       slot.ToString("o"),
                Queued:     found?.Queued     ?? 0,
                Processing: found?.Processing ?? 0,
                Retrying:   found?.Retrying   ?? 0,
                Failed:     found?.Failed     ?? 0,
                Sent:       found?.Sent       ?? 0
            );
        }).ToList();

        return buckets;
    }

    // ─── Queue ───────────────────────────────────────────────────────────────

    public async Task<QueueSnapshotDto> GetQueueSnapshotAsync(Guid organizationId, CancellationToken ct = default)
    {
        var db          = _redis.GetDatabase();
        var queueLength = (int)await db.ListLengthAsync("notification_queue");

        var server        = _redis.GetServer(_redis.GetEndPoints()[0]);
        var workersOnline = server.Keys(pattern: "worker:heartbeat:*").Count();

        var counts = await _context.Notifications
            .Where(n => n.OrganizationId == organizationId)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Processing = g.Count(n => n.Status == NotificationStatus.Processing),
                Retrying   = g.Count(n => n.Status == NotificationStatus.Retrying),
                DeadLetter = g.Count(n => n.Status == NotificationStatus.DeadLetter),
            })
            .FirstOrDefaultAsync(ct);

        var throughput            = workersOnline > 0 ? workersOnline * 50 : 1;
        var estimatedDrainMinutes = Math.Round((double)queueLength / throughput, 1);

        var oldestPending = await _context.Notifications
            .Where(n => n.OrganizationId == organizationId && n.Status == NotificationStatus.Pending)
            .OrderBy(n => n.CreatedAt)
            .Select(n => (DateTime?)n.CreatedAt)
            .FirstOrDefaultAsync(ct);

        var oldestPendingSeconds = oldestPending.HasValue
            ? (long)(DateTime.UtcNow - oldestPending.Value).TotalSeconds
            : 0L;

        return new QueueSnapshotDto(
            Pending:               queueLength,
            Processing:            counts?.Processing ?? 0,
            Retrying:              counts?.Retrying   ?? 0,
            DeadLetter:            counts?.DeadLetter ?? 0,
            AvgWaitMs:             184,
            WorkersActive:         workersOnline,
            WorkerCapacity:        200,
            MaxConcurrency:        50,
            ThroughputPerMinute:   throughput,
            OldestPendingSeconds:  oldestPendingSeconds,
            EstimatedDrainMinutes: estimatedDrainMinutes
        );
    }

    // ─── Campaigns ───────────────────────────────────────────────────────────

    public async Task<CampaignAnalyticsDto> GetCampaignAnalyticsAsync(Guid organizationId, CancellationToken ct = default)
    {
        var todayUtc = DateTime.UtcNow.Date;

        var summary = await _context.Campaigns
            .Where(c => c.OrganizationId == organizationId && c.DeletedAt == null)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Running        = g.Count(c => c.Status == CampaignStatus.Running),
                Scheduled      = g.Count(c => c.Status == CampaignStatus.Scheduled),
                Drafts         = g.Count(c => c.Status == CampaignStatus.Draft),
                CompletedToday = g.Count(c => c.Status == CampaignStatus.Completed && c.CompletedAt >= todayUtc),
                Largest        = g.Max(c => (int?)c.TotalRecipients) ?? 0,
                Average        = (int)(g.Average(c => (double?)c.TotalRecipients) ?? 0),
            })
            .FirstOrDefaultAsync(ct);

        // FIX 1: pull to memory before TimeSpan arithmetic — EF cannot translate this to SQL
        var completedTimings = await _context.Campaigns
            .Where(c => c.OrganizationId == organizationId &&
                        c.Status == CampaignStatus.Completed &&
                        c.StartedAt != null && c.CompletedAt != null)
            .Select(c => new { c.StartedAt, c.CompletedAt })
            .ToListAsync(ct);

        var avgCompletion = completedTimings.Count > 0
            ? completedTimings.Average(c => (c.CompletedAt!.Value - c.StartedAt!.Value).TotalMinutes)
            : 0.0;

        // FIX 2: pull StartedAt to memory before subtracting DateTime.UtcNow
        var runningStartTimes = await _context.Campaigns
            .Where(c => c.OrganizationId == organizationId &&
                        c.Status == CampaignStatus.Running &&
                        c.StartedAt != null)
            .Select(c => c.StartedAt)
            .ToListAsync(ct);

        var longestRunning = runningStartTimes.Count > 0
            ? runningStartTimes.Max(s => (DateTime.UtcNow - s!.Value).TotalMinutes)
            : 0.0;

        // FIX 3: Notification has no CampaignId — join via CampaignRecipients
        var recent = await _context.Campaigns
            .Where(c => c.OrganizationId == organizationId && c.DeletedAt == null)
            .OrderByDescending(c => c.CreatedAt)
            .Take(5)
            .Select(c => new
            {
                c.Id,
                c.Title,
                c.TotalRecipients,
                c.Status,
                c.ScheduledAt,
                SentCount = _context.CampaignRecipients
                    .Where(r => r.CampaignId == c.Id && r.NotificationId != null)
                    .Join(_context.Notifications,
                        r => r.NotificationId,
                        n => n.Id,
                        (r, n) => n)
                    .Count(n => n.Status == NotificationStatus.Sent),
            })
            .ToListAsync(ct);

        var recentDtos = recent.Select(c => new RecentCampaignDto(
            c.Id.ToString(),
            c.Title,
            c.TotalRecipients,
            c.Status.ToString(),
            c.TotalRecipients > 0
                ? Math.Round((double)c.SentCount / c.TotalRecipients * 100, 1)
                : (double?)null,
            c.ScheduledAt?.ToString("o")
        )).ToList();

        return new CampaignAnalyticsDto(
            Running:                  summary?.Running        ?? 0,
            Scheduled:                summary?.Scheduled      ?? 0,
            Drafts:                   summary?.Drafts         ?? 0,
            CompletedToday:           summary?.CompletedToday ?? 0,
            LargestCampaign:          summary?.Largest        ?? 0,
            AverageCampaignSize:      summary?.Average        ?? 0,
            AverageCompletionMinutes: Math.Round(avgCompletion,  1),
            LongestRunningMinutes:    Math.Round(longestRunning, 1),
            Recent:                   recentDtos
        );
    }

    // ─── Failures ────────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<FailureDto>> GetFailuresAsync(Guid organizationId, CancellationToken ct = default)
    {
        var failures = await _context.Notifications
            .Where(n => n.OrganizationId == organizationId &&
                       (n.Status == NotificationStatus.Failed ||
                        n.Status == NotificationStatus.DeadLetter))
            .OrderByDescending(n => n.CreatedAt)
            .Take(10)
            .Select(n => new
            {
                n.PublicId,
                n.Type,
                n.RetryCount,
                n.CreatedAt,
                LatestLog = _context.NotificationLogs
                    .Where(l => l.NotificationId == n.Id)
                    .OrderByDescending(l => l.CreatedAt)
                    .Select(l => new { l.Provider, l.Response })
                    .FirstOrDefault(),
            })
            .ToListAsync(ct);

        return failures.Select(f =>
        {
            var response    = f.LatestLog?.Response ?? string.Empty;
            var failureType = ClassifyFailure(response, f.RetryCount);
            var suggested   = SuggestAction(failureType);

            return new FailureDto(
                NotificationId:  f.PublicId.ToString(),
                Title:           f.Type,
                Provider:        f.LatestLog?.Provider ?? "Unknown",
                FailureType:     failureType,
                Reason:          string.IsNullOrEmpty(response) ? "No response recorded" : response[..Math.Min(response.Length, 120)],
                RetryCount:      f.RetryCount,
                Campaign:        null,
                SuggestedAction: suggested,
                OccurredAt:      f.CreatedAt
            );
        }).ToList();
    }

    // ─── Activity ────────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<ActivityEventDto>> GetActivityAsync(Guid organizationId, CancellationToken ct = default)
    {
        var events = new List<ActivityEventDto>();

        var notifications = await _context.Notifications
            .Where(n => n.OrganizationId == organizationId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(20)
            .Select(n => new { n.PublicId, n.Type, n.RecipientEmail, n.Status, n.CreatedAt })
            .ToListAsync(ct);

        events.AddRange(notifications.Select(n => new ActivityEventDto(
            Id:        $"notif-{n.PublicId}",
            Type:      MapNotificationStatus(n.Status),
            Title:     MapNotificationTitle(n.Status, n.Type),
            Subtitle:  n.RecipientEmail,
            Timestamp: n.CreatedAt
        )));

        var campaigns = await _context.Campaigns
            .Where(c => c.OrganizationId == organizationId &&
                       (c.StartedAt != null || c.CompletedAt != null))
            .OrderByDescending(c => c.UpdatedAt)
            .Take(10)
            .Select(c => new { c.Id, c.Title, c.TotalRecipients, c.StartedAt, c.CompletedAt, c.Status })
            .ToListAsync(ct);

        events.AddRange(campaigns.Select(c => new ActivityEventDto(
            Id:        $"campaign-{c.Id}",
            Type:      "campaign",
            Title:     c.Status == CampaignStatus.Completed ? "Campaign completed" : "Campaign started",
            Subtitle:  $"{c.Title} · {FormatCount(c.TotalRecipients)} recipients",
            Timestamp: c.CompletedAt ?? c.StartedAt ?? DateTime.UtcNow
        )));

        var keys = await _context.ApiKeys
            .Where(k => k.OrganizationId == organizationId)
            .OrderByDescending(k => k.CreatedAt)
            .Take(5)
            .Select(k => new { k.Id, k.Name, k.CreatedAt })
            .ToListAsync(ct);

        events.AddRange(keys.Select(k => new ActivityEventDto(
            Id:        $"key-{k.Id}",
            Type:      "key",
            Title:     "API key created",
            Subtitle:  k.Name,
            Timestamp: k.CreatedAt
        )));

        var invites = await _context.OrgInvites
            .Where(i => i.OrganizationId == organizationId && i.IsAccepted)
            .OrderByDescending(i => i.CreatedAt)
            .Take(5)
            .Select(i => new { i.Id, i.Email, InvitedAt = i.CreatedAt })
            .ToListAsync(ct);

        events.AddRange(invites.Select(i => new ActivityEventDto(
            Id:        $"invite-{i.Id}",
            Type:      "invite",
            Title:     "Member joined",
            Subtitle:  i.Email,
            Timestamp: i.InvitedAt
        )));

        return events
            .OrderByDescending(e => e.Timestamp)
            .Take(20)
            .ToList();
    }

    // ─── Infrastructure ──────────────────────────────────────────────────────

    public async Task<InfrastructureHealthDto> GetInfrastructureAsync(Guid organizationId, CancellationToken ct = default)
    {
        var dbLatency    = await PingDatabaseAsync(ct);
        var redisLatency = await PingRedisAsync();

        var server        = _redis.GetServer(_redis.GetEndPoints()[0]);
        var workersOnline = server.Keys(pattern: "worker:heartbeat:*").Count();

        // FIX: same as GetHealthAsync — use IsSuccess, not string-matching "success".
        var recentLogs = await _context.NotificationLogs
            .Where(l => _context.Notifications
                .Where(n => n.OrganizationId == organizationId)
                .Select(n => n.Id)
                .Contains(l.NotificationId))
            .OrderByDescending(l => l.CreatedAt)
            .Take(100)
            .Select(l => l.IsSuccess)
            .ToListAsync(ct);

        var providerSuccessRate = recentLogs.Count == 0 ? 100.0
            : Math.Round((double)recentLogs.Count(s => s) / recentLogs.Count * 100, 1);

        var uptime    = DateTime.UtcNow - _startedAt;
        var uptimeStr = uptime.TotalDays >= 1
            ? $"{(int)uptime.TotalDays}d {uptime.Hours}h"
            : $"{uptime.Hours}h {uptime.Minutes}m";

        return new InfrastructureHealthDto(
            Database: new DbComponentDto(dbLatency < 100 ? "healthy" : "degraded", dbLatency),
            Redis:    new RedisComponentDto(redisLatency < 50 ? "healthy" : "degraded", redisLatency),
            Workers:  new WorkerComponentDto(workersOnline > 0 ? "healthy" : "down", workersOnline, 200),
            Provider: new ProviderComponentDto("Resend", providerSuccessRate >= 98 ? "healthy" : "degraded", providerSuccessRate, 480),
            Api:      new ApiComponentDto("healthy", uptimeStr)
        );
    }

    // ─── Delivery Funnel ─────────────────────────────────────────────────────

    public async Task<DeliveryFunnelDto> GetDeliveryFunnelAsync(Guid organizationId, CancellationToken ct = default)
    {
        var db          = _redis.GetDatabase();
        var queueLength = (int)await db.ListLengthAsync("notification_queue");

        var counts = await _context.Notifications
            .Where(n => n.OrganizationId == organizationId)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Processing = g.Count(n => n.Status == NotificationStatus.Processing),
                Sent       = g.Count(n => n.Status == NotificationStatus.Sent),
                Failed     = g.Count(n => n.Status == NotificationStatus.Failed),
                DeadLetter = g.Count(n => n.Status == NotificationStatus.DeadLetter),
            })
            .FirstOrDefaultAsync(ct);

        return new DeliveryFunnelDto(
            Queued:     queueLength,
            Processing: counts?.Processing ?? 0,
            Sent:       counts?.Sent       ?? 0,
            Failed:     counts?.Failed     ?? 0,
            DeadLetter: counts?.DeadLetter ?? 0
        );
    }

    // ─── Providers ───────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<ProviderAnalyticsDto>> GetProvidersAsync(Guid organizationId, CancellationToken ct = default)
    {
        var todayUtc = DateTime.UtcNow.Date;

        var logs = await _context.NotificationLogs
            .Where(l => _context.Notifications
                .Where(n => n.OrganizationId == organizationId && n.CreatedAt >= todayUtc)
                .Select(n => n.Id)
                .Contains(l.NotificationId))
            .GroupBy(l => l.Provider)
            .Select(g => new
            {
                Provider  = g.Key,
                Total     = g.Count(),
                
                Successes = g.Count(l => l.IsSuccess),
            })
            .ToListAsync(ct);

        return logs.Select(l => new ProviderAnalyticsDto(
            Provider:     l.Provider,
            SuccessRate:  l.Total == 0 ? 100 : Math.Round((double)l.Successes / l.Total * 100, 1),
            AvgLatencyMs: 480,
            SentToday:    l.Total
        )).ToList();
    }

    // ─── Usage ───────────────────────────────────────────────────────────────

    public async Task<UsageDto> GetUsageAsync(Guid organizationId, CancellationToken ct = default)
    {
        var limit = 100_000;

        var sent = await _context.Notifications
            .CountAsync(n => n.OrganizationId == organizationId &&
                             n.Status == NotificationStatus.Sent, ct);

        return new UsageDto(
            Plan:               "Free",
            NotificationsUsed:  sent,
            NotificationsLimit: limit,
            ApiCallsToday:      0,
            UsagePct:           Math.Round((double)sent / limit * 100, 1)
        );
    }

    // ─── Private helpers ─────────────────────────────────────────────────────

    private async Task<long> PingDatabaseAsync(CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        try { await _context.Database.ExecuteSqlRawAsync("SELECT 1", ct); }
        catch (Exception ex) { _logger.LogWarning(ex, "Database ping failed"); return 9999; }
        return sw.ElapsedMilliseconds;
    }

    private async Task<long> PingRedisAsync()
    {
        var sw = Stopwatch.StartNew();
        try { await _redis.GetDatabase().PingAsync(); }
        catch (Exception ex) { _logger.LogWarning(ex, "Redis ping failed"); return 9999; }
        return sw.ElapsedMilliseconds;
    }

    private static string ClassifyFailure(string response, int retryCount)
    {
        if (retryCount >= 5)                                                        return "dead_letter";
        if (response.Contains("timeout",    StringComparison.OrdinalIgnoreCase))   return "smtp_timeout";
        if (response.Contains("rate limit", StringComparison.OrdinalIgnoreCase) ||
            response.Contains("429",        StringComparison.OrdinalIgnoreCase))   return "rate_limited";
        if (response.Contains("invalid",    StringComparison.OrdinalIgnoreCase) ||
            response.Contains("550",        StringComparison.OrdinalIgnoreCase))   return "invalid_email";
        return "unknown";
    }

    private static string SuggestAction(string failureType) => failureType switch
    {
        "smtp_timeout"  => "Provider latency detected. Automatic retry in progress.",
        "rate_limited"  => "Provider rate limit hit. Retry will back off automatically.",
        "invalid_email" => "Recipient address is invalid. Remove from your list.",
        "dead_letter"   => "Maximum retries exhausted. Manual review required.",
        _               => "Check provider logs for details.",
    };

    private static string MapNotificationStatus(NotificationStatus status) => status switch
    {
        NotificationStatus.Sent       => "sent",
        NotificationStatus.Failed     => "failed",
        NotificationStatus.Retrying   => "retry",
        NotificationStatus.DeadLetter => "dlq",
        _                             => "sent",
    };

    private static string MapNotificationTitle(NotificationStatus status, string type) => status switch
    {
        NotificationStatus.Sent       => $"{type} sent",
        NotificationStatus.Failed     => $"{type} failed",
        NotificationStatus.Retrying   => $"Retrying {type}",
        NotificationStatus.DeadLetter => $"Dead letter — {type}",
        _                             => type,
    };

    private static string FormatCount(int n) =>
        n >= 1_000_000 ? $"{n / 1_000_000.0:F1}M"
        : n >= 1_000   ? $"{n / 1_000.0:F0}k"
        : n.ToString();
}