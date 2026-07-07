using NotificationHub.Infrastructure.Auth;

namespace NotificationHub.Api.Middlewares;

public class OrgMembershipMiddleware
{
    private readonly RequestDelegate _next;

    public OrgMembershipMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, CurrentOrganization currentOrganization)
    {
        // Only check authenticated requests that have an org claim
        if (context.User.Identity?.IsAuthenticated == true &&
            currentOrganization.OrganizationId is not null)
        {
            var isActive = await currentOrganization.IsMemberActiveAsync();

            if (!isActive)
            {
                context.Response.StatusCode = 403;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(
                    "{\"error\":\"access_revoked\",\"message\":\"Your access to this organization has been revoked.\"}");
                return;
            }
        }

        await _next(context);
    }
}