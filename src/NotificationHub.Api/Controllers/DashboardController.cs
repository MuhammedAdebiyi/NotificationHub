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
    private readonly ICurrentUser _currentUser;

    public DashboardController(
        AppDbContext context,
        IConnectionMultiplexer redis,
        ICurrentUser currentUser)
    {
        _context = context;
        _redis = redis;
        _currentUser = currentUser;
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats(CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;

        var query = _context.Notifications.AsQueryable();
        if (userId.HasValue)
            query = query.Where(n => n.UserId == userId.Value);

        var totalSent = await query
            .CountAsync(n => n.Status == NotificationStatus.Sent, cancellationToken);

        var pending = await query
            .CountAsync(n => n.Status == NotificationStatus.Pending, cancellationToken);

        var failed = await query
            .CountAsync(n => n.Status == NotificationStatus.Failed ||
                             n.Status == NotificationStatus.DeadLetter, cancellationToken);

        var total = await query.CountAsync(cancellationToken);
        var successRate = total == 0 ? 0.0 : Math.Round((double)totalSent / total * 100, 1);

        var db = _redis.GetDatabase();
        var queueLength = await db.ListLengthAsync("notification_queue");

        var activeUsers = await _context.Users
            .CountAsync(u => !u.IsDeleted, cancellationToken);

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
        var userId = _currentUser.UserId;

        var query = _context.Notifications.AsQueryable();
        if (userId.HasValue)
            query = query.Where(n => n.UserId == userId.Value);

        var activity = await query
            .OrderByDescending(n => n.CreatedAt)
            .Take(10)
            .Select(n => new
            {
                id = n.PublicId,
                type = n.Type,
                channel = n.Channel.ToString(),
                status = n.Status.ToString(),
                createdAt = n.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return Ok(activity);
    }
}