namespace NotificationHub.Application.Abstractions;

public interface IEmailProviderFactory
{
    IEmailProvider GetProvider(Guid organizationId);
    Task<IEmailProvider> GetProviderAsync(Guid organizationId, CancellationToken cancellationToken = default);
}
