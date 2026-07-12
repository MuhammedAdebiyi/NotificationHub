namespace NotificationHub.Application.Features.Analytics.DTOs;

// ─── Health ──────────────────────────────────────────────────────────────────

public sealed record AnalyticsHealthDto(
    string OverallStatus,          // "healthy" | "warning" | "critical"
    double SuccessRate,
    long   QueueLatencyMs,
    int    WorkersOnline,
    string? IncidentMessage,
    IReadOnlyList<ComponentHealthDto> Components
);

public sealed record ComponentHealthDto(
    string  Name,
    string  Status,           // "healthy" | "degraded" | "down"
    long?   LatencyMs,
    int?    Workers,
    double? SuccessRate
);

// ─── Overview ────────────────────────────────────────────────────────────────

public sealed record AnalyticsOverviewDto(
    int    NotificationsSent,
    double NotificationsSentDelta,    // % vs same window yesterday
    double SuccessRate,
    double SuccessRateDelta,
    int    QueueDepth,
    int    WorkersActive,
    long   AvgSendTimeMs,
    long   P95SendTimeMs,
    int    DeadLetters,
    int    DeadLettersNeedingReview,
    int    CampaignsRunning,
    int    CampaignsScheduled,
    long   ApiCallsToday,
    string Plan,
    double PlanUsagePct,
    double EstimatedQueueDrainMinutes,
    long   OldestPendingSeconds
);

// ─── Timeline ────────────────────────────────────────────────────────────────

public sealed record TimelinePointDto(
    string Hour,          // "13:00"
    int    Queued,
    int    Processing,
    int    Retrying,
    int    Failed,
    int    Sent
);

// ─── Queue ───────────────────────────────────────────────────────────────────

public sealed record QueueSnapshotDto(
    int    Pending,
    int    Processing,
    int    Retrying,
    int    DeadLetter,
    long   AvgWaitMs,
    int    WorkersActive,
    int    WorkerCapacity,
    int    MaxConcurrency,
    int    ThroughputPerMinute,
    long   OldestPendingSeconds,
    double EstimatedDrainMinutes
);

// ─── Campaigns ───────────────────────────────────────────────────────────────

public sealed record CampaignAnalyticsDto(
    int    Running,
    int    Scheduled,
    int    Drafts,
    int    CompletedToday,
    int    LargestCampaign,
    int    AverageCampaignSize,
    double AverageCompletionMinutes,
    double LongestRunningMinutes,
    IReadOnlyList<RecentCampaignDto> Recent
);

public sealed record RecentCampaignDto(
    string  Id,
    string  Title,
    int     RecipientCount,
    string  Status,
    double? ProgressPercent,
    string? ScheduledAt
);

// ─── Failures ────────────────────────────────────────────────────────────────

public sealed record FailureDto(
    string NotificationId,
    string Title,
    string Provider,
    string FailureType,       // "smtp_timeout" | "rate_limited" | "invalid_email" | "dead_letter" | "unknown"
    string Reason,
    int    RetryCount,
    string? Campaign,
    string SuggestedAction,
    DateTime OccurredAt
);

// ─── Activity ────────────────────────────────────────────────────────────────

public sealed record ActivityEventDto(
    string   Id,
    string   Type,            // "sent" | "failed" | "retry" | "campaign" | "key" | "invite" | "dlq"
    string   Title,
    string   Subtitle,
    DateTime Timestamp
);

// ─── Infrastructure ──────────────────────────────────────────────────────────

public sealed record InfrastructureHealthDto(
    DbComponentDto      Database,
    RedisComponentDto   Redis,
    WorkerComponentDto  Workers,
    ProviderComponentDto Provider,
    ApiComponentDto     Api
);

public sealed record DbComponentDto(string Status, long LatencyMs);
public sealed record RedisComponentDto(string Status, long LatencyMs);
public sealed record WorkerComponentDto(string Status, int Online, int Capacity);
public sealed record ProviderComponentDto(string Name, string Status, double SuccessRate, long AvgLatencyMs);
public sealed record ApiComponentDto(string Status, string Uptime);

// ─── Delivery Funnel ─────────────────────────────────────────────────────────

public sealed record DeliveryFunnelDto(
    int Queued,
    int Processing,
    int Sent,
    int Failed,
    int DeadLetter
);

// ─── Providers ───────────────────────────────────────────────────────────────

public sealed record ProviderAnalyticsDto(
    string Provider,
    double SuccessRate,
    long   AvgLatencyMs,
    int    SentToday
);

// ─── Usage ───────────────────────────────────────────────────────────────────

public sealed record UsageDto(
    string Plan,
    int    NotificationsUsed,
    int    NotificationsLimit,
    long   ApiCallsToday,
    double UsagePct
);