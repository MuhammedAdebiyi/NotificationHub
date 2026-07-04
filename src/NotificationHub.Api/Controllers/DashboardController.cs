using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NotificationHub.Domain.Enums;
using NotificationHub.Infrastructure.Persistence;
using NotificationHub.Shared.Abstractions;
using StackExchange.Redis;

namespace NotificationHub.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/dashboard")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IConnectionMultiplexer _redis;
    private readonly ICurrentOrganization _currentOrg;

    public DashboardController(
        AppDbContext context,
        IConnectionMultiplexer redis,
        ICurrentOrganization currentOrg)
    {
        _context = context;
        _redis = redis;
        _currentOrg = currentOrg;
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats(CancellationToken cancellationToken)
    {
        if (!_currentOrg.IsAuthenticated || _currentOrg.OrganizationId is null)
            return Unauthorized(new { error = "No organization context." });

        var orgId = _currentOrg.OrganizationId.Value;

        var totalSent = await _context.Notifications
            .CountAsync(n => n.OrganizationId == orgId &&
                             n.Status == NotificationStatus.Sent, cancellationToken);

        var pending = await _context.Notifications
            .CountAsync(n => n.OrganizationId == orgId &&
                             n.Status == NotificationStatus.Pending, cancellationToken);

        var failed = await _context.Notifications
            .CountAsync(n => n.OrganizationId == orgId &&
                            (n.Status == NotificationStatus.Failed ||
                             n.Status == NotificationStatus.DeadLetter), cancellationToken);

        var total = await _context.Notifications
            .CountAsync(n => n.OrganizationId == orgId, cancellationToken);

        var successRate = total == 0 ? 0.0 : Math.Round((double)totalSent / total * 100, 1);

        var db = _redis.GetDatabase();
        var queueLength = await db.ListLengthAsync("notification_queue");

        var activeUsers = await _context.OrganizationMembers
            .CountAsync(m => m.OrganizationId == orgId, cancellationToken);

        return Ok(new
        {
            totalSent,
            pending,
            failed,
            successRate,
            queueLength,
            activeUsers
        });
    }

    [HttpGet("activity")]
    public async Task<IActionResult> GetActivity(CancellationToken cancellationToken)
    {
        if (!_currentOrg.IsAuthenticated || _currentOrg.OrganizationId is null)
            return Unauthorized(new { error = "No organization context." });

        var orgId = _currentOrg.OrganizationId.Value;

        var activity = await _context.Notifications
            .Where(n => n.OrganizationId == orgId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(10)
            .Select(n => new
            {
                id = n.PublicId,
                label = n.Type,
                channel = n.Channel.ToString(),
                status = n.Status.ToString(),
                timestamp = n.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return Ok(activity);
    }
}