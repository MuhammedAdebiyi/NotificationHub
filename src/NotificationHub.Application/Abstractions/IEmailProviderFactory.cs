namespace NotificationHub.Application.Abstractions;

public interface IEmailProviderFactory
{
    IEmailProvider GetProvider(Guid organizationId);
    Task<IEmailProvider> GetProviderAsync(Guid organizationId, CancellationToken cancellationToken = default);
    Task<DomainsResult> ListDomainsWithHealthAsync(Guid organizationId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EmailProviderInfo>> GetAllProvidersAsync(Guid organizationId, CancellationToken cancellationToken = default);
    Task<IEmailProvider> GetProviderWithFallbackAsync(Guid organizationId, string? preferredProviderType = null, CancellationToken cancellationToken = default);
    Task<ProviderSendResult> SendWithFallbackAsync(Guid organizationId, EmailMessage message, CancellationToken cancellationToken = default);
}

// Thrown when every configured provider failed to accept the message.
// Failures are ordered (default provider first) so logs show the real chain.
public class ProviderSendFailedException : Exception
{
    public IReadOnlyList<ProviderSendFailure> Failures { get; }

    public ProviderSendFailedException(IReadOnlyList<ProviderSendFailure> failures)
        : base(failures.Count == 0
            ? "No email provider accepted the message."
            : $"All providers failed: {string.Join("; ", failures.Select(f => $"{f.ProviderType}: {f.Error}"))}")
    {
        Failures = failures;
    }
}

public record ProviderSendFailure(string ProviderType, string Error);

public record ProviderSendResult(
    string ProviderType,
    string MessageId,
    IReadOnlyList<ProviderSendFailure> Failures);

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
