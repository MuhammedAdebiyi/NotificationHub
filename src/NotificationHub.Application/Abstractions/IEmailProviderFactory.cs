namespace NotificationHub.Application.Abstractions;

public interface IEmailProviderFactory
{
    IEmailProvider GetProvider(Guid organizationId);
    Task<IEmailProvider> GetProviderAsync(Guid organizationId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProviderDomain>> ListDomainsAsync(Guid organizationId, CancellationToken cancellationToken = default);
}
