using NotificationHub.Application.Features.Analytics.DTOs;

namespace NotificationHub.Application.Abstractions;

public interface IAnalyticsService
{
    Task<AnalyticsHealthDto>      GetHealthAsync(Guid organizationId, CancellationToken ct = default);
    Task<AnalyticsOverviewDto>    GetOverviewAsync(Guid organizationId, CancellationToken ct = default);
    Task<IReadOnlyList<TimelinePointDto>> GetTimelineAsync(Guid organizationId, CancellationToken ct = default);
    Task<QueueSnapshotDto>        GetQueueSnapshotAsync(Guid organizationId, CancellationToken ct = default);
    Task<CampaignAnalyticsDto>    GetCampaignAnalyticsAsync(Guid organizationId, CancellationToken ct = default);
    Task<IReadOnlyList<FailureDto>> GetFailuresAsync(Guid organizationId, CancellationToken ct = default);
    Task<IReadOnlyList<ActivityEventDto>> GetActivityAsync(Guid organizationId, CancellationToken ct = default);
    Task<InfrastructureHealthDto> GetInfrastructureAsync(Guid organizationId, CancellationToken ct = default);
    Task<DeliveryFunnelDto>       GetDeliveryFunnelAsync(Guid organizationId, CancellationToken ct = default);
    Task<IReadOnlyList<ProviderAnalyticsDto>> GetProvidersAsync(Guid organizationId, CancellationToken ct = default);
    Task<UsageDto>                GetUsageAsync(Guid organizationId, CancellationToken ct = default);
}