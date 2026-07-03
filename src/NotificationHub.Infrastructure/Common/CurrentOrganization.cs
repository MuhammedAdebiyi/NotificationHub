using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using NotificationHub.Shared.Abstractions;

namespace NotificationHub.Infrastructure.Common;

public class CurrentOrganization : ICurrentOrganization
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentOrganization(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public Guid? OrganizationId
    {
        get
        {
            var claim = User?.FindFirstValue("org_id");
            return Guid.TryParse(claim, out var id) ? id : null;
        }
    }

    public string? Role => User?.FindFirstValue(ClaimTypes.Role);

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;
}