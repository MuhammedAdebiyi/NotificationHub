using Microsoft.EntityFrameworkCore;
using NotificationHub.Application.Abstractions;
using NotificationHub.Domain.Entities;
using NotificationHub.Infrastructure.Persistence;

namespace NotificationHub.Infrastructure.Repositories;

public class OrganizationRepository : IOrganizationRepository
{
    private readonly AppDbContext _context;

    public OrganizationRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Organization?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _context.Organizations
            .FirstOrDefaultAsync(o => o.Id == id && o.DeletedAt == null, cancellationToken);

    public async Task<Organization?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default) =>
        await _context.Organizations
            .FirstOrDefaultAsync(o => o.Slug == slug && o.DeletedAt == null, cancellationToken);

    public async Task AddAsync(Organization organization, CancellationToken cancellationToken = default) =>
        await _context.Organizations.AddAsync(organization, cancellationToken);

    public async Task AddMemberAsync(OrganizationMember member, CancellationToken cancellationToken = default) =>
        await _context.OrganizationMembers.AddAsync(member, cancellationToken);
    public async Task<IReadOnlyList<OrganizationMember>> GetAllMembershipsAsync(
    Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.OrganizationMembers
            .Include(m => m.Organization)
            .Where(m => m.UserId == userId)
            .ToListAsync(cancellationToken);
    }
    public async Task<OrganizationMember?> GetMembershipByUserIdAsync(
    Guid userId, CancellationToken cancellationToken = default)
    => await _context.OrganizationMembers
        .FirstOrDefaultAsync(m => m.UserId == userId, cancellationToken);

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        await _context.SaveChangesAsync(cancellationToken);
}