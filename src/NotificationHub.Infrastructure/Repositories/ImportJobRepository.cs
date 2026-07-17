using Microsoft.EntityFrameworkCore;
using NotificationHub.Application.Abstractions;
using NotificationHub.Domain.Entities;
using NotificationHub.Domain.Enums;
using NotificationHub.Infrastructure.Persistence;

namespace NotificationHub.Infrastructure.Repositories;

public class ImportJobRepository : IImportJobRepository
{
    private readonly AppDbContext _context;

    public ImportJobRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<ImportJob> AddAsync(ImportJob job, CancellationToken cancellationToken = default)
    {
        _context.ImportJobs.Add(job);
        await _context.SaveChangesAsync(cancellationToken);
        return job;
    }

    public async Task<ImportJob?> GetByIdAsync(
        Guid id, Guid organizationId, CancellationToken cancellationToken = default)
    {
        return await _context.ImportJobs
            .FirstOrDefaultAsync(j => j.Id == id && j.OrganizationId == organizationId, cancellationToken);
    }

    public async Task<List<ImportJob>> GetPendingAsync(CancellationToken cancellationToken = default)
    {
        return await _context.ImportJobs
            .Where(j => EF.Property<ImportJobStatus>(j, "Status") == ImportJobStatus.Pending)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<ImportJob>> GetRunningAsync(CancellationToken cancellationToken = default)
    {
        return await _context.ImportJobs
            .Where(j => EF.Property<ImportJobStatus>(j, "Status") == ImportJobStatus.Running)
            .ToListAsync(cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}