using Microsoft.EntityFrameworkCore;
using NotificationHub.Application.Abstractions;
using NotificationHub.Domain.Entities;
using NotificationHub.Infrastructure.Persistence;

namespace NotificationHub.Infrastructure.Repositories;

public class ApiKeyRepository : IApiKeyRepository
{
    private readonly AppDbContext _context;

    public ApiKeyRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<ApiKey>> GetByOrgAsync(
        Guid organizationId, CancellationToken cancellationToken = default)
    {
        return await _context.ApiKeys
            .Where(k => k.OrganizationId == organizationId
                     && k.IsActive
                     && k.DeletedAt == null)
            .OrderByDescending(k => k.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<ApiKey?> GetByIdAsync(
        Guid id, Guid organizationId, CancellationToken cancellationToken = default)
    {
        return await _context.ApiKeys
            .FirstOrDefaultAsync(k => k.Id == id
                                   && k.OrganizationId == organizationId
                                   && k.DeletedAt == null,
                                   cancellationToken);
    }
    public async Task<IReadOnlyList<ApiKey>> GetByPrefixAsync(
    string prefix, CancellationToken cancellationToken = default)
    {
        return await _context.ApiKeys
            .Where(k => k.KeyPrefix == prefix && k.IsActive && k.DeletedAt == null)
            .ToListAsync(cancellationToken);
    }

    public async Task StampLastUsedAsync(
        Guid id, DateTime lastUsedAt, CancellationToken cancellationToken = default)
    {
        await _context.ApiKeys
            .Where(k => k.Id == id)
            .ExecuteUpdateAsync(s => s.SetProperty(k => k.LastUsedAt, lastUsedAt), cancellationToken);
    }
    public async Task<ApiKey?> GetByHashAsync(
        string keyHash, CancellationToken cancellationToken = default)
    {
        return await _context.ApiKeys
            .Include(k => k.Organization)
            .FirstOrDefaultAsync(k => k.KeyHash == keyHash
                                   && k.IsActive
                                   && k.DeletedAt == null,
                                   cancellationToken);
    }

    public async Task AddAsync(ApiKey apiKey, CancellationToken cancellationToken = default)
    {
        await _context.ApiKeys.AddAsync(apiKey, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}