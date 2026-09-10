using NotificationHub.Domain.Entities;

namespace NotificationHub.Application.Abstractions;

public interface IEmailProviderConfigRepository
{
    Task<EmailProviderConfig?> GetByOrgAsync(Guid organizationId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EmailProviderConfig>> GetAllByOrgAsync(Guid organizationId, CancellationToken cancellationToken = default);
    Task<EmailProviderConfig?> GetDefaultAsync(Guid organizationId, CancellationToken cancellationToken = default);
    Task<EmailProviderConfig?> GetByOrgAndTypeAsync(Guid organizationId, string providerType, CancellationToken cancellationToken = default);
    Task AddAsync(EmailProviderConfig config, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
