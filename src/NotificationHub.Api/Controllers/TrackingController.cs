using MediatR;
using Microsoft.AspNetCore.Mvc;
using NotificationHub.Application.Abstractions;
using NotificationHub.Domain.Entities;
using NotificationHub.Infrastructure.Persistence;

namespace NotificationHub.Api.Controllers;

[ApiController]
[Route("api/v1/track")]
public class TrackingController : ControllerBase
{
    private readonly AppDbContext _context;

    private const string TransparentPixel =
        "R0lGODlhAQABAIAAAAAAAP///yH5BAEAAAAALAAAAAABAAEAAAIBRAA7";

    public TrackingController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("open/{notificationId:guid}")]
    public async Task<IActionResult> TrackOpen(Guid notificationId)
    {
        var notification = await _context.Notifications.FindAsync(notificationId);
        if (notification is not null && notification.OpenedAt is null)
        {
            notification.OpenedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        return File(
            Convert.FromBase64String(TransparentPixel),
            "image/gif");
    }
}
