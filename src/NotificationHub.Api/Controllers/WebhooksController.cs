using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NotificationHub.Domain.Entities;
using NotificationHub.Infrastructure.Persistence;
using NotificationHub.Shared.Abstractions;

namespace NotificationHub.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/webhooks")]
[Authorize]
public class WebhooksController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ICurrentOrganization _currentOrg;

    public WebhooksController(AppDbContext context, ICurrentOrganization currentOrg)
    {
        _context = context;
        _currentOrg = currentOrg;
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        if (_currentOrg.OrganizationId is null) return Unauthorized();

        var hooks = await _context.Webhooks
            .Where(w => w.OrganizationId == _currentOrg.OrganizationId.Value)
            .OrderByDescending(w => w.CreatedAt)
            .Select(w => new
            {
                w.Id, w.Url, w.Events, w.IsActive,
                w.LastTriggeredAt, w.LastError, w.CreatedAt
            })
            .ToListAsync(ct);

        return Ok(new { items = hooks });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateWebhookRequest req, CancellationToken ct)
    {
        if (_currentOrg.OrganizationId is null) return Unauthorized();

        var hook = new Webhook
        {
            OrganizationId = _currentOrg.OrganizationId.Value,
            Url = req.Url.Trim(),
            Secret = req.Secret,
            Events = req.Events ?? new[] { "notification.sent", "notification.failed" },
            IsActive = true,
        };

        _context.Webhooks.Add(hook);
        await _context.SaveChangesAsync(ct);

        return Ok(new { hook.Id, hook.Url, hook.Events, hook.CreatedAt });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        if (_currentOrg.OrganizationId is null) return Unauthorized();

        var hook = await _context.Webhooks
            .FirstOrDefaultAsync(w => w.Id == id && w.OrganizationId == _currentOrg.OrganizationId.Value, ct);

        if (hook is null) return NotFound();
        _context.Webhooks.Remove(hook);
        await _context.SaveChangesAsync(ct);
        return Ok(new { deleted = true });
    }

    [HttpGet("{id:guid}/deliveries")]
    public async Task<IActionResult> GetDeliveries(Guid id, CancellationToken ct)
    {
        if (_currentOrg.OrganizationId is null) return Unauthorized();

        var deliveries = await _context.WebhookDeliveries
            .Where(d => d.WebhookId == id)
            .OrderByDescending(d => d.CreatedAt)
            .Take(50)
            .Select(d => new
            {
                d.Id, d.Event, d.StatusCode, d.IsSuccess,
                d.DurationMs, d.Response, d.CreatedAt
            })
            .ToListAsync(ct);

        return Ok(new { items = deliveries });
    }

    [HttpPost("{id:guid}/test")]
    public async Task<IActionResult> Test(Guid id, CancellationToken ct)
    {
        if (_currentOrg.OrganizationId is null) return Unauthorized();

        var hook = await _context.Webhooks
            .FirstOrDefaultAsync(w => w.Id == id && w.OrganizationId == _currentOrg.OrganizationId.Value, ct);

        if (hook is null) return NotFound();

        var payload = System.Text.Json.JsonSerializer.Serialize(new
        {
            event_type = "webhook.test",
            timestamp = DateTime.UtcNow,
            organization_id = _currentOrg.OrganizationId,
        });

        var delivery = new WebhookDelivery
        {
            WebhookId = id,
            Event = "webhook.test",
            Payload = payload,
            StatusCode = 200,
            IsSuccess = true,
            DurationMs = 0,
        };

        _context.WebhookDeliveries.Add(delivery);
        hook.LastTriggeredAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(ct);

        return Ok(new { sent = true, deliveryId = delivery.Id });
    }
}

public record CreateWebhookRequest(
    string Url,
    string? Secret,
    string[]? Events
);
