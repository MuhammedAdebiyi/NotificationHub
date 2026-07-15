using Microsoft.EntityFrameworkCore;
using NotificationHub.Application.Abstractions;
using NotificationHub.Domain.Entities;
using NotificationHub.Infrastructure.Persistence;

namespace NotificationHub.Infrastructure.Repositories;

public class TemplateRepository : ITemplateRepository
{
    private readonly AppDbContext _context;

    public TemplateRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Template?> GetByIdAsync(Guid id, Guid organizationId, CancellationToken cancellationToken = default)
        => await _context.Templates
            .FirstOrDefaultAsync(t => t.Id == id && t.OrganizationId == organizationId && t.DeletedAt == null, cancellationToken);

    public async Task<(IReadOnlyList<Template> Items, int TotalCount)> GetPagedAsync(
        Guid organizationId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _context.Templates
            .Where(t => t.OrganizationId == organizationId && t.DeletedAt == null)
            .OrderByDescending(t => t.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task AddAsync(Template template, CancellationToken cancellationToken = default)
        => await _context.Templates.AddAsync(template, cancellationToken);

    public async Task<int> CountByOrgAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        return await _context.Templates
            .CountAsync(t => t.OrganizationId == organizationId && t.DeletedAt == null, cancellationToken);
    }
    public async Task<int> CountByUserAsync(Guid organizationId, Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Templates
            .CountAsync(t => t.OrganizationId == organizationId && t.CreatedByUserId == userId && t.DeletedAt == null, cancellationToken);
    }
    public async Task DeleteAsync(Guid id, Guid organizationId, CancellationToken cancellationToken = default)
    {
        var template = await _context.Templates
            .FirstOrDefaultAsync(t => t.Id == id && t.OrganizationId == organizationId && t.DeletedAt == null, cancellationToken);
        if (template is not null)
            template.DeletedAt = DateTime.UtcNow;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => await _context.SaveChangesAsync(cancellationToken);
}