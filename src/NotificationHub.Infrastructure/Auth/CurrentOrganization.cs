using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using NotificationHub.Infrastructure.Persistence;
using NotificationHub.Shared.Abstractions;

namespace NotificationHub.Infrastructure.Auth;

public class CurrentOrganization : ICurrentOrganization
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly AppDbContext _context;

    public CurrentOrganization(IHttpContextAccessor httpContextAccessor, AppDbContext context)
    {
        _httpContextAccessor = httpContextAccessor;
        _context = context;
    }

    public Guid? OrganizationId
    {
        get
        {
            var value = _httpContextAccessor.HttpContext?.User.FindFirstValue("org_id");
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    public string? Role =>
        _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Role);

    public bool IsAuthenticated =>
        _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated == true;

    // Call this from middleware to verify membership is still active in DB
    public async Task<bool> IsMemberActiveAsync(CancellationToken cancellationToken = default)
    {
        var userId = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userId, out var userGuid)) return false;
        if (OrganizationId is null) return false;

        var member = await _context.OrganizationMembers
            .FirstOrDefaultAsync(m =>
                m.UserId == userGuid &&
                m.OrganizationId == OrganizationId.Value,
                cancellationToken);

        return member is not null && member.Role != "revoked";
    }
}