using NotificationHub.Domain.Entities;

namespace NotificationHub.Application.Abstractions;

public interface IApiKeyRepository
{
    Task<IReadOnlyList<ApiKey>> GetByOrgAsync(Guid organizationId, CancellationToken cancellationToken = default);
    Task<ApiKey?> GetByIdAsync(Guid id, Guid organizationId, CancellationToken cancellationToken = default);
    Task<ApiKey?> GetByHashAsync(string keyHash, CancellationToken cancellationToken = default);
    Task AddAsync(ApiKey apiKey, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ApiKey>> GetByPrefixAsync(string prefix, CancellationToken cancellationToken = default);
    Task StampLastUsedAsync(Guid id, DateTime lastUsedAt, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}