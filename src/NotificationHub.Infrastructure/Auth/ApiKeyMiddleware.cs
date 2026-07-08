using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NotificationHub.Application.Abstractions;

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

        context.Items["OrganizationId"] = matchedKey.OrganizationId;
        context.Items["OrgRole"] = "service";

        await _next(context);
    }
}