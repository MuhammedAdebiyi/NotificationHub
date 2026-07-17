using Microsoft.EntityFrameworkCore;
using NotificationHub.Application.Abstractions;
using NotificationHub.Domain.Entities;
using NotificationHub.Infrastructure.Persistence;

namespace NotificationHub.Infrastructure.Repositories;

public class DataSourceRepository : IDataSourceRepository
{
    private readonly AppDbContext _context;

    public DataSourceRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<DataSource> AddAsync(DataSource dataSource, CancellationToken cancellationToken = default)
    {
        _context.DataSources.Add(dataSource);
        await _context.SaveChangesAsync(cancellationToken);
        return dataSource;
    }

    public async Task<DataSource?> GetByIdAsync(
        Guid id, Guid organizationId, CancellationToken cancellationToken = default)
    {
        return await _context.DataSources
            .FirstOrDefaultAsync(d => d.Id == id && d.OrganizationId == organizationId, cancellationToken);
    }

    public async Task<(List<DataSource> Items, int TotalCount)> GetPagedAsync(
        Guid organizationId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _context.DataSources
            .Where(d => d.OrganizationId == organizationId)
            .OrderByDescending(d => d.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task DeleteAsync(DataSource dataSource, CancellationToken cancellationToken = default)
    {
        _context.DataSources.Remove(dataSource);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> HasImportJobsAsync(Guid dataSourceId, CancellationToken cancellationToken = default)
    {
        return await _context.ImportJobs
            .AnyAsync(j => j.DataSourceId == dataSourceId, cancellationToken);
    }
}