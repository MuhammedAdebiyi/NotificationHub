namespace NotificationHub.Application.Abstractions;

public interface IEmailProviderDomains
{
    Task<IReadOnlyList<ProviderDomain>> ListDomainsAsync(CancellationToken cancellationToken = default);
}

public record ProviderDomain(
    string Id,
    string Domain,
    string Status, // "pending" or "verified"
    DateTime? VerifiedAt,
    bool DeliverabilityReady
);
