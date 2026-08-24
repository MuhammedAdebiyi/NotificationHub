using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using NotificationHub.Application.Abstractions;

namespace NotificationHub.Infrastructure.Auth;

public class ApiKeyMiddleware
{
    private readonly RequestDelegate _next;
    private const string ApiKeyHeader = "X-Api-Key";
    public const string AuthenticationScheme = "ApiKey";

    public ApiKeyMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
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

        var keyString = rawKey.ToString();

        // Must be at least 12 chars to have a valid prefix
        if (keyString.Length < 12)
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { error = "Invalid API key." });
            return;
        }

        using var scope = context.RequestServices.CreateScope();
        var apiKeyRepository = scope.ServiceProvider.GetRequiredService<IApiKeyRepository>();

        // Get all active keys for this org prefix — narrows to 1 row in practice
        var prefix = keyString[..12];
        var candidates = await apiKeyRepository.GetByPrefixAsync(prefix);

        var matchedKey = candidates.FirstOrDefault(k =>
            ApiKeyGenerator.Verify(keyString, k.KeyHash));

        if (matchedKey is null)
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { error = "Invalid API key." });
            return;
        }

        // Stamp last used — fire and forget, don't block the request
        _ = Task.Run(async () =>
        {
            using var stampScope = context.RequestServices.CreateScope();
            var repo = stampScope.ServiceProvider.GetRequiredService<IApiKeyRepository>();
            await repo.StampLastUsedAsync(matchedKey.Id, DateTime.UtcNow);
        });

        // Build a ClaimsPrincipal so [Authorize] and CurrentOrganization work.
        // API keys are org-scoped (not user-scoped), so we use the org ID as the
        // NameIdentifier claim and set role to "service" to distinguish from JWT users.
        var orgId = matchedKey.OrganizationId.ToString();
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, orgId),
            new Claim("org_id", orgId),
            new Claim(ClaimTypes.Role, "service"),
        };
        var identity = new ClaimsIdentity(claims, AuthenticationScheme);
        context.User = new ClaimsPrincipal(identity);

        await _next(context);
    }
}
