using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using NotificationHub.Shared.Abstractions;

namespace NotificationHub.Infrastructure.Auth;

public class CurrentOrganization : ICurrentOrganization
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentOrganization(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? OrganizationId
    {
        get
        {
            var ctx = _httpContextAccessor.HttpContext;
            if (ctx is null) return null;

            // API key auth — set by ApiKeyMiddleware
            if (ctx.Items.TryGetValue("OrganizationId", out var itemValue)
                && itemValue is Guid orgId)
                return orgId;

            // JWT auth — set by JwtBearer middleware
            var claim = ctx.User.FindFirstValue("org_id");
            return Guid.TryParse(claim, out var jwtOrgId) ? jwtOrgId : null;
        }
    }

    public string? Role
    {
        get
        {
            var ctx = _httpContextAccessor.HttpContext;
            if (ctx is null) return null;

            if (ctx.Items.TryGetValue("OrgRole", out var role))
                return role?.ToString();

            return ctx.User.FindFirstValue(ClaimTypes.Role);
        }
    }

    public bool IsAuthenticated =>
        OrganizationId.HasValue;
}