using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NotificationHub.Application.Abstractions;
using NotificationHub.Infrastructure.Email.Providers;

namespace NotificationHub.Infrastructure.Email;

public class UserEmailProviderFactory : IEmailProviderFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<UserEmailProviderFactory> _logger;

    public UserEmailProviderFactory(IServiceProvider serviceProvider, ILogger<UserEmailProviderFactory> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public IEmailProvider GetProvider(Guid organizationId)
    {
        return GetProviderAsync(organizationId).GetAwaiter().GetResult();
    }

    public async Task<IEmailProvider> GetProviderAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var configRepo = scope.ServiceProvider.GetRequiredService<IEmailProviderConfigRepository>();
        var encryptionService = scope.ServiceProvider.GetRequiredService<IEncryptionService>();

        var config = await configRepo.GetByOrgAsync(organizationId, cancellationToken);

        if (config is null)
        {
            _logger.LogDebug("No email provider configured for org {OrgId}, using platform default", organizationId);
            return _serviceProvider.GetRequiredService<IEmailProvider>();
        }

        try
        {
            var apiKey = encryptionService.Decrypt(config.EncryptedApiKey);

            return config.ProviderType.ToLowerInvariant() switch
            {
                "resend" => new ResendAdapter(
                    new HttpClient(),
                    apiKey,
                    scope.ServiceProvider.GetRequiredService<ILogger<ResendAdapter>>()),

                "sendbyte" => new SendByteAdapter(
                    new HttpClient(),
                    apiKey,
                    scope.ServiceProvider.GetRequiredService<ILogger<SendByteAdapter>>()),

                "sendgrid" => new SendGridAdapter(
                    new HttpClient(),
                    apiKey,
                    scope.ServiceProvider.GetRequiredService<ILogger<SendGridAdapter>>()),

                "brevo" => new BrevoAdapter(
                    new HttpClient(),
                    apiKey,
                    scope.ServiceProvider.GetRequiredService<ILogger<BrevoAdapter>>()),

                "smtp" => new SmtpAdapter(
                    apiKey,
                    scope.ServiceProvider.GetRequiredService<ILogger<SmtpAdapter>>()),

                _ => throw new InvalidOperationException($"Unsupported email provider: {config.ProviderType}")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create email provider for org {OrgId}, falling back to default", organizationId);
            return _serviceProvider.GetRequiredService<IEmailProvider>();
        }
    }

    public async Task<IReadOnlyList<ProviderDomain>> ListDomainsAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var configRepo = scope.ServiceProvider.GetRequiredService<IEmailProviderConfigRepository>();
        var encryptionService = scope.ServiceProvider.GetRequiredService<IEncryptionService>();

        var config = await configRepo.GetByOrgAsync(organizationId, cancellationToken);

        if (config is null)
        {
            _logger.LogDebug("No email provider configured for org {OrgId}, cannot list domains", organizationId);
            return Array.Empty<ProviderDomain>();
        }

        try
        {
            var apiKey = encryptionService.Decrypt(config.EncryptedApiKey);

            IEmailProviderDomains? domainService = config.ProviderType.ToLowerInvariant() switch
            {
                "resend" => new ResendAdapter(
                    new HttpClient(),
                    apiKey,
                    scope.ServiceProvider.GetRequiredService<ILogger<ResendAdapter>>()),

                "sendbyte" => new SendByteAdapter(
                    new HttpClient(),
                    apiKey,
                    scope.ServiceProvider.GetRequiredService<ILogger<SendByteAdapter>>()),

                "sendgrid" => new SendGridAdapter(
                    new HttpClient(),
                    apiKey,
                    scope.ServiceProvider.GetRequiredService<ILogger<SendGridAdapter>>()),

                "brevo" => new BrevoAdapter(
                    new HttpClient(),
                    apiKey,
                    scope.ServiceProvider.GetRequiredService<ILogger<BrevoAdapter>>()),

                _ => null
            };

            if (domainService is null)
            {
                _logger.LogDebug("Provider {ProviderType} does not support domain listing", config.ProviderType);
                return Array.Empty<ProviderDomain>();
            }

            return await domainService.ListDomainsAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list domains for org {OrgId}", organizationId);
            return Array.Empty<ProviderDomain>();
        }
    }
}
