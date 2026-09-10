namespace NotificationHub.Application.Abstractions;

public interface IEmailProviderFactory
{
    IEmailProvider GetProvider(Guid organizationId);
    Task<IEmailProvider> GetProviderAsync(Guid organizationId, CancellationToken cancellationToken = default);
    Task<DomainsResult> ListDomainsWithHealthAsync(Guid organizationId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EmailProviderInfo>> GetAllProvidersAsync(Guid organizationId, CancellationToken cancellationToken = default);
    Task<IEmailProvider> GetProviderWithFallbackAsync(Guid organizationId, string? preferredProviderType = null, CancellationToken cancellationToken = default);
}

public record EmailProviderInfo(
    Guid Id,
    string ProviderType,
    bool IsActive,
    bool IsDefault,
    DateTime CreatedAt
);

public record DomainsResult(
    IReadOnlyList<ProviderDomain> Domains,
    IReadOnlyList<ProviderHealthCheck> ProviderHealth
);

public record ProviderHealthCheck(
    string ProviderType,
    Guid ProviderId,
    bool IsHealthy,
    string? Error = null
);
