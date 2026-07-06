using Microsoft.EntityFrameworkCore;
using NotificationHub.Application.Abstractions;
using NotificationHub.Domain.Entities;
using NotificationHub.Infrastructure.Persistence;

namespace NotificationHub.Infrastructure.Repositories;

public class OrgInviteRepository : IOrgInviteRepository
{
    private readonly AppDbContext _context;

    public OrgInviteRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<OrgInvite>> GetPendingByOrgAsync(
        Guid organizationId, CancellationToken cancellationToken = default)
    {
        return await _context.OrgInvites
            .Where(i => i.OrganizationId == organizationId
                     && !i.IsAccepted
                     && i.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<OrgInvite?> GetByIdAsync(
        Guid id, Guid organizationId, CancellationToken cancellationToken = default)
    {
        return await _context.OrgInvites
            .FirstOrDefaultAsync(i => i.Id == id && i.OrganizationId == organizationId, cancellationToken);
    }

    public async Task<OrgInvite?> GetByTokenAsync(
        string token, CancellationToken cancellationToken = default)
    {
        return await _context.OrgInvites
            .Include(i => i.Organization)
            .FirstOrDefaultAsync(i => i.Token == token, cancellationToken);
    }

    public async Task<bool> PendingInviteExistsAsync(
        Guid organizationId, string email, CancellationToken cancellationToken = default)
    {
        return await _context.OrgInvites
            .AnyAsync(i => i.OrganizationId == organizationId
                        && i.Email == email
                        && !i.IsAccepted
                        && i.ExpiresAt > DateTime.UtcNow, cancellationToken);
    }

    public async Task AddAsync(OrgInvite invite, CancellationToken cancellationToken = default)
    {
        await _context.OrgInvites.AddAsync(invite, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}