using Microsoft.EntityFrameworkCore;
using NotificationHub.Application.Abstractions;
using NotificationHub.Domain.Entities;
using NotificationHub.Infrastructure.Persistence;

namespace NotificationHub.Infrastructure.Repositories;

public class OrganizationMemberRepository : IOrganizationMemberRepository
{
    private readonly AppDbContext _context;

    public OrganizationMemberRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<OrganizationMember>> GetByOrgAsync(
        Guid organizationId, CancellationToken cancellationToken = default)
    {
        return await _context.OrganizationMembers
            .Include(m => m.User)
            .Where(m => m.OrganizationId == organizationId)
            .OrderBy(m => m.JoinedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<OrganizationMember?> GetByIdAsync(
        Guid id, Guid organizationId, CancellationToken cancellationToken = default)
    {
        return await _context.OrganizationMembers
            .Include(m => m.User)
            .FirstOrDefaultAsync(m => m.Id == id && m.OrganizationId == organizationId, cancellationToken);
    }

    public async Task<OrganizationMember?> GetByUserAndOrgAsync(
        Guid userId, Guid organizationId, CancellationToken cancellationToken = default)
    {
        return await _context.OrganizationMembers
            .FirstOrDefaultAsync(m => m.UserId == userId && m.OrganizationId == organizationId, cancellationToken);
    }

    public async Task AddAsync(OrganizationMember member, CancellationToken cancellationToken = default)
    {
        await _context.OrganizationMembers.AddAsync(member, cancellationToken);
    }

    public Task RemoveAsync(OrganizationMember member, CancellationToken cancellationToken = default)
    {
        _context.OrganizationMembers.Remove(member);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}