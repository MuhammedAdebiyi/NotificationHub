namespace NotificationHub.Application.Abstractions;

public interface IEmailProviderDomains
{
    Task<IReadOnlyList<ProviderDomain>> ListDomainsAsync(CancellationToken cancellationToken = default);
}

public record ProviderDomain(
    string Id,
    string Domain,
    string ProviderType = "",
    string Status = "pending",
    DateTime? VerifiedAt = null,
    bool DeliverabilityReady = false
);
