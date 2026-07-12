using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NotificationHub.Application.Abstractions;
using NotificationHub.Shared.Abstractions;

namespace NotificationHub.Api.Controllers;

/// <summary>
/// Homepage-only data. Analytics live in AnalyticsController.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/dashboard")]
[Authorize]
public sealed class DashboardController : ControllerBase
{
    private readonly IAnalyticsService    _analytics;
    private readonly ICurrentOrganization _currentOrg;

    public DashboardController(IAnalyticsService analytics, ICurrentOrganization currentOrg)
    {
        _analytics  = analytics;
        _currentOrg = currentOrg;
    }

    /// <summary>
    /// KPI summary cards for the dashboard homepage.
    /// Delegates to AnalyticsService — no DB or Redis here.
    /// </summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats(CancellationToken ct)
    {
        if (_currentOrg.OrganizationId is null) return Unauthorized();
        return Ok(await _analytics.GetOverviewAsync(_currentOrg.OrganizationId.Value, ct));
    }

    /// <summary>
    /// 7-day notification volume for the homepage chart.
    /// </summary>
    [HttpGet("volume")]
    public async Task<IActionResult> GetVolume(CancellationToken ct)
    {
        if (_currentOrg.OrganizationId is null) return Unauthorized();
        return Ok(await _analytics.GetTimelineAsync(_currentOrg.OrganizationId.Value, ct));
    }

    /// <summary>
    /// Recent activity feed for the homepage.
    /// </summary>
    [HttpGet("activity")]
    public async Task<IActionResult> GetActivity(CancellationToken ct)
    {
        if (_currentOrg.OrganizationId is null) return Unauthorized();
        return Ok(await _analytics.GetActivityAsync(_currentOrg.OrganizationId.Value, ct));
    }
}