using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NotificationHub.Application.Abstractions;
using NotificationHub.Shared.Abstractions;

namespace NotificationHub.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/analytics")]
[Authorize]
public sealed class AnalyticsController : ControllerBase
{
    private readonly IAnalyticsService   _analytics;
    private readonly ICurrentOrganization _currentOrg;

    public AnalyticsController(IAnalyticsService analytics, ICurrentOrganization currentOrg)
    {
        _analytics  = analytics;
        _currentOrg = currentOrg;
    }

    [HttpGet("health")]
    public async Task<IActionResult> GetHealth(CancellationToken ct)
    {
        if (_currentOrg.OrganizationId is null) return Unauthorized();
        return Ok(await _analytics.GetHealthAsync(_currentOrg.OrganizationId.Value, ct));
    }

    [HttpGet("overview")]
    public async Task<IActionResult> GetOverview(CancellationToken ct)
    {
        if (_currentOrg.OrganizationId is null) return Unauthorized();
        return Ok(await _analytics.GetOverviewAsync(_currentOrg.OrganizationId.Value, ct));
    }

    [HttpGet("timeline")]
    public async Task<IActionResult> GetTimeline(CancellationToken ct)
    {
        if (_currentOrg.OrganizationId is null) return Unauthorized();
        return Ok(await _analytics.GetTimelineAsync(_currentOrg.OrganizationId.Value, ct));
    }

    [HttpGet("queue")]
    public async Task<IActionResult> GetQueue(CancellationToken ct)
    {
        if (_currentOrg.OrganizationId is null) return Unauthorized();
        return Ok(await _analytics.GetQueueSnapshotAsync(_currentOrg.OrganizationId.Value, ct));
    }

    [HttpGet("campaigns")]
    public async Task<IActionResult> GetCampaigns(CancellationToken ct)
    {
        if (_currentOrg.OrganizationId is null) return Unauthorized();
        return Ok(await _analytics.GetCampaignAnalyticsAsync(_currentOrg.OrganizationId.Value, ct));
    }

    [HttpGet("failures")]
    public async Task<IActionResult> GetFailures(CancellationToken ct)
    {
        if (_currentOrg.OrganizationId is null) return Unauthorized();
        return Ok(await _analytics.GetFailuresAsync(_currentOrg.OrganizationId.Value, ct));
    }

    [HttpGet("activity")]
    public async Task<IActionResult> GetActivity(CancellationToken ct)
    {
        if (_currentOrg.OrganizationId is null) return Unauthorized();
        return Ok(await _analytics.GetActivityAsync(_currentOrg.OrganizationId.Value, ct));
    }

    [HttpGet("infrastructure")]
    public async Task<IActionResult> GetInfrastructure(CancellationToken ct)
    {
        if (_currentOrg.OrganizationId is null) return Unauthorized();
        return Ok(await _analytics.GetInfrastructureAsync(_currentOrg.OrganizationId.Value, ct));
    }

    [HttpGet("delivery-funnel")]
    public async Task<IActionResult> GetDeliveryFunnel(CancellationToken ct)
    {
        if (_currentOrg.OrganizationId is null) return Unauthorized();
        return Ok(await _analytics.GetDeliveryFunnelAsync(_currentOrg.OrganizationId.Value, ct));
    }

    [HttpGet("providers")]
    public async Task<IActionResult> GetProviders(CancellationToken ct)
    {
        if (_currentOrg.OrganizationId is null) return Unauthorized();
        return Ok(await _analytics.GetProvidersAsync(_currentOrg.OrganizationId.Value, ct));
    }

    [HttpGet("usage")]
    public async Task<IActionResult> GetUsage(CancellationToken ct)
    {
        if (_currentOrg.OrganizationId is null) return Unauthorized();
        return Ok(await _analytics.GetUsageAsync(_currentOrg.OrganizationId.Value, ct));
    }
}