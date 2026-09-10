using Microsoft.EntityFrameworkCore;
using NotificationHub.Application.Abstractions;
using NotificationHub.Domain.Entities;
using NotificationHub.Infrastructure.Persistence;

namespace NotificationHub.Infrastructure.Repositories;

public class EmailProviderConfigRepository : IEmailProviderConfigRepository
{
    private readonly AppDbContext _context;

    public EmailProviderConfigRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<EmailProviderConfig?> GetByOrgAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        return await _context.EmailProviderConfigs
            .FirstOrDefaultAsync(c => c.OrganizationId == organizationId, cancellationToken);
    }

    public async Task<IReadOnlyList<EmailProviderConfig>> GetAllByOrgAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        return await _context.EmailProviderConfigs
            .Where(c => c.OrganizationId == organizationId && c.IsActive)
            .OrderByDescending(c => c.IsDefault)
            .ThenByDescending(c => c.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<EmailProviderConfig?> GetDefaultAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        return await _context.EmailProviderConfigs
            .FirstOrDefaultAsync(c => c.OrganizationId == organizationId && c.IsDefault, cancellationToken);
    }

    public async Task<EmailProviderConfig?> GetByOrgAndTypeAsync(Guid organizationId, string providerType, CancellationToken cancellationToken = default)
    {
        return await _context.EmailProviderConfigs
            .FirstOrDefaultAsync(c => c.OrganizationId == organizationId && c.ProviderType == providerType, cancellationToken);
    }

    public async Task AddAsync(EmailProviderConfig config, CancellationToken cancellationToken = default)
    {
        _context.EmailProviderConfigs.Add(config);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
