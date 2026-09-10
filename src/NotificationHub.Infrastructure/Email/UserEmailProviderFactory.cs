using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NotificationHub.Application.Abstractions;
using NotificationHub.Domain.Entities;
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

        var config = await configRepo.GetDefaultAsync(organizationId, cancellationToken)
                     ?? await configRepo.GetByOrgAsync(organizationId, cancellationToken);

        if (config is null)
        {
            _logger.LogDebug("No email provider configured for org {OrgId}, using platform default", organizationId);
            return _serviceProvider.GetRequiredService<IEmailProvider>();
        }

        return CreateProvider(config, scope.ServiceProvider, encryptionService);
    }

    public async Task<IEmailProvider> GetProviderWithFallbackAsync(
        Guid organizationId,
        string? preferredProviderType = null,
        CancellationToken cancellationToken = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var configRepo = scope.ServiceProvider.GetRequiredService<IEmailProviderConfigRepository>();
        var encryptionService = scope.ServiceProvider.GetRequiredService<IEncryptionService>();

        var allConfigs = await configRepo.GetAllByOrgAsync(organizationId, cancellationToken);

        if (allConfigs.Count == 0)
        {
            _logger.LogDebug("No email provider configured for org {OrgId}, using platform default", organizationId);
            return _serviceProvider.GetRequiredService<IEmailProvider>();
        }

        // Try preferred provider first, then default, then others
        var ordered = allConfigs
            .OrderByDescending(c => c.ProviderType.Equals(preferredProviderType, StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(c => c.IsDefault)
            .ToList();

        IEmailProvider? lastProvider = null;
        foreach (var config in ordered)
        {
            try
            {
                var provider = CreateProvider(config, scope.ServiceProvider, encryptionService);
                lastProvider = provider;
                return provider;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to create {ProviderType} provider for org {OrgId}, trying next",
                    config.ProviderType, organizationId);
            }
        }

        _logger.LogWarning("All providers failed for org {OrgId}, using platform default", organizationId);
        return _serviceProvider.GetRequiredService<IEmailProvider>();
    }

    public async Task<IReadOnlyList<EmailProviderInfo>> GetAllProvidersAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var configRepo = scope.ServiceProvider.GetRequiredService<IEmailProviderConfigRepository>();

        var configs = await configRepo.GetAllByOrgAsync(organizationId, cancellationToken);

        return configs.Select(c => new EmailProviderInfo(
            c.Id,
            c.ProviderType,
            c.IsActive,
            c.IsDefault,
            c.CreatedAt
        )).ToList();
    }

    public async Task<IReadOnlyList<ProviderDomain>> ListDomainsAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var configRepo = scope.ServiceProvider.GetRequiredService<IEmailProviderConfigRepository>();
        var encryptionService = scope.ServiceProvider.GetRequiredService<IEncryptionService>();

        var configs = await configRepo.GetAllByOrgAsync(organizationId, cancellationToken);
        var allDomains = new List<ProviderDomain>();

        foreach (var config in configs)
        {
            try
            {
                var apiKey = encryptionService.Decrypt(config.EncryptedApiKey);
                var domainService = CreateDomainService(config, apiKey, scope.ServiceProvider);
                if (domainService is null) continue;

                var domains = await domainService.ListDomainsAsync(cancellationToken);
                allDomains.AddRange(domains);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to list domains for {ProviderType} on org {OrgId}",
                    config.ProviderType, organizationId);
            }
        }

        return allDomains;
    }

    private IEmailProvider CreateProvider(
        EmailProviderConfig config,
        IServiceProvider serviceProvider,
        IEncryptionService encryptionService)
    {
        var apiKey = encryptionService.Decrypt(config.EncryptedApiKey);

        return config.ProviderType.ToLowerInvariant() switch
        {
            "resend" => new ResendAdapter(
                new HttpClient(),
                apiKey,
                serviceProvider.GetRequiredService<ILogger<ResendAdapter>>()),

            "sendbyte" => new SendByteAdapter(
                new HttpClient(),
                apiKey,
                serviceProvider.GetRequiredService<ILogger<SendByteAdapter>>()),

            "sendgrid" => new SendGridAdapter(
                new HttpClient(),
                apiKey,
                serviceProvider.GetRequiredService<ILogger<SendGridAdapter>>()),

            "brevo" => new BrevoAdapter(
                new HttpClient(),
                apiKey,
                serviceProvider.GetRequiredService<ILogger<BrevoAdapter>>()),

            "smtp" => new SmtpAdapter(
                apiKey,
                serviceProvider.GetRequiredService<ILogger<SmtpAdapter>>()),

            _ => throw new InvalidOperationException($"Unsupported email provider: {config.ProviderType}")
        };
    }

    private IEmailProviderDomains? CreateDomainService(
        EmailProviderConfig config,
        string apiKey,
        IServiceProvider serviceProvider)
    {
        return config.ProviderType.ToLowerInvariant() switch
        {
            "resend" => new ResendAdapter(
                new HttpClient(),
                apiKey,
                serviceProvider.GetRequiredService<ILogger<ResendAdapter>>()),

            "sendbyte" => new SendByteAdapter(
                new HttpClient(),
                apiKey,
                serviceProvider.GetRequiredService<ILogger<SendByteAdapter>>()),

            "sendgrid" => new SendGridAdapter(
                new HttpClient(),
                apiKey,
                serviceProvider.GetRequiredService<ILogger<SendGridAdapter>>()),

            "brevo" => new BrevoAdapter(
                new HttpClient(),
                apiKey,
                serviceProvider.GetRequiredService<ILogger<BrevoAdapter>>()),

            _ => null
        };
    }
}
