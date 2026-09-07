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
                _ => throw new InvalidOperationException($"Unsupported email provider: {config.ProviderType}")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create email provider for org {OrgId}, falling back to default", organizationId);
            return _serviceProvider.GetRequiredService<IEmailProvider>();
        }
    }
}
