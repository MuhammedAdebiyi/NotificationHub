using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NotificationHub.Application.Abstractions;
using NotificationHub.Infrastructure.Persistence;

namespace NotificationHub.Api.Controllers;

// Unsubscribe endpoints used by campaign emails.
//
// GET  → renders a confirm page (mail scanners prefetch GET, so this never
//        unsubscribes by itself).
// POST → records the opt-out. This is also what Gmail/Yahoo one-click
//        unsubscribe sends (List-Unsubscribe-Post).
[ApiController]
[Route("api/v1/unsubscribe")]
[AllowAnonymous]
public class UnsubscribeController : ControllerBase
{
    private readonly IUnsubscribeTokenService _tokens;
    private readonly AppDbContext _context;

    public UnsubscribeController(IUnsubscribeTokenService tokens, AppDbContext context)
    {
        _tokens = tokens;
        _context = context;
    }

    [HttpGet("{token}")]
    public IActionResult Confirm(string token)
    {
        if (_tokens.ReadToken(token) is null)
            return BadRequest(Page("NotificationHub", "This unsubscribe link is invalid or expired."));

        return Content(Page("NotificationHub",
            "Click the button below to stop receiving campaign emails from this sender."),
            "text/html");
    }

    [HttpPost("{token}")]
    public async Task<IActionResult> Unsubscribe(string token, CancellationToken ct)
    {
        var payload = _tokens.ReadToken(token);
        if (payload is null)
            return BadRequest(Page("NotificationHub", "This unsubscribe link is invalid or expired."));

        var email = payload.Email.Trim().ToLowerInvariant();
        var exists = await _context.RecipientOptOuts.AnyAsync(o =>
            o.OrganizationId == payload.OrganizationId && o.Email == email, ct);

        if (!exists)
        {
            _context.RecipientOptOuts.Add(new Domain.Entities.RecipientOptOut
            {
                Id = Guid.NewGuid(),
                OrganizationId = payload.OrganizationId,
                Email = email,
                OptedOutAt = DateTime.UtcNow,
            });
            await _context.SaveChangesAsync(ct);
        }

        return Content(Page("Done", "You've been unsubscribed. You won't receive further campaign emails from this sender."),
            "text/html");
    }

    private static string Page(string title, string message) => $$"""
        <!doctype html>
        <html>
        <head>
          <meta charset="utf-8">
          <meta name="viewport" content="width=device-width, initial-scale=1">
          <title>{{title}}</title>
          <style>
            body { font-family: system-ui, sans-serif; background: #F6F5F2; color: #15131B; display: flex; align-items: center; justify-content: center; min-height: 100vh; margin: 0; }
            .card { background: #fff; border-radius: 12px; padding: 32px 40px; max-width: 420px; text-align: center; box-shadow: 0 2px 12px rgba(0,0,0,.06); }
            h1 { font-size: 18px; margin: 0 0 8px; }
            p { font-size: 14px; color: #555; line-height: 1.5; margin: 0 0 20px; }
            button { background: #6D28D9; color: #fff; border: 0; border-radius: 8px; padding: 10px 24px; font-size: 14px; cursor: pointer; }
          </style>
        </head>
        <body>
          <div class="card">
            <h1>{{title}}</h1>
            <p>{{message}}</p>
            <form method="post">
              <button type="submit">Unsubscribe</button>
            </form>
          </div>
        </body>
        </html>
        """;
}
