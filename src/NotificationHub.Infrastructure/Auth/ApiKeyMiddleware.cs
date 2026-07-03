using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NotificationHub.Infrastructure.Persistence;

namespace NotificationHub.Infrastructure.Auth;

public class ApiKeyMiddleware
{
    private readonly RequestDelegate _next;
    private const string ApiKeyHeader = "X-Api-Key";

    public ApiKeyMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // If already authenticated via JWT, skip API key check
        if (context.User.Identity?.IsAuthenticated == true)
        {
            await _next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue(ApiKeyHeader, out var rawKey))
        {
            await _next(context);
            return;
        }

        using var scope = context.RequestServices.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Load all active keys for the org — we need to BCrypt.Verify each one
        // This is intentionally O(n) per request — key count per org is tiny (< 10)
        var activeKeys = await db.ApiKeys
            .Where(k => k.IsActive && k.DeletedAt == null)
            .ToListAsync();

        var matchedKey = activeKeys.FirstOrDefault(k =>
            ApiKeyGenerator.Verify(rawKey.ToString(), k.KeyHash));

        if (matchedKey is null)
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { error = "Invalid API key." });
            return;
        }

        // Stamp last used
        matchedKey.LastUsedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        // Inject org context into HttpContext items so ICurrentOrganization can read it
        context.Items["OrganizationId"] = matchedKey.OrganizationId;
        context.Items["OrgRole"] = "service"; // service-to-service calls get "service" role

        await _next(context);
    }
}