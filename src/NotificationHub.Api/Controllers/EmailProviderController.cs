using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NotificationHub.Application.Abstractions;
using NotificationHub.Domain.Entities;
using NotificationHub.Shared.Abstractions;

namespace NotificationHub.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/org/email-provider")]
[Authorize]
public class EmailProviderController : ControllerBase
{
    private readonly IEmailProviderConfigRepository _configRepository;
    private readonly IEmailProviderFactory _emailProviderFactory;
    private readonly ICurrentOrganization _currentOrg;
    private readonly IEncryptionService _encryptionService;

    public EmailProviderController(
        IEmailProviderConfigRepository configRepository,
        IEmailProviderFactory emailProviderFactory,
        ICurrentOrganization currentOrg,
        IEncryptionService encryptionService)
    {
        _configRepository = configRepository;
        _emailProviderFactory = emailProviderFactory;
        _currentOrg = currentOrg;
        _encryptionService = encryptionService;
    }

    [HttpGet]
    public async Task<IActionResult> GetConfig(CancellationToken cancellationToken)
    {
        if (_currentOrg.OrganizationId is null)
            return Unauthorized(new { error = "No organization context." });

        if (_currentOrg.Role == "member" || _currentOrg.Role == "revoked")
            return StatusCode(403, new { error = "permission_denied" });

        var config = await _configRepository.GetByOrgAsync(
            _currentOrg.OrganizationId.Value, cancellationToken);

        if (config is null)
            return Ok(new { configured = false });

        return Ok(new
        {
            configured = true,
            providerType = config.ProviderType,
            senderEmail = config.SenderEmail,
            isActive = config.IsActive,
            createdAt = config.CreatedAt,
        });
    }

    [HttpGet("domains")]
    public async Task<IActionResult> ListDomains(CancellationToken cancellationToken)
    {
        if (_currentOrg.OrganizationId is null)
            return Unauthorized(new { error = "No organization context." });

        if (_currentOrg.Role == "member" || _currentOrg.Role == "revoked")
            return StatusCode(403, new { error = "permission_denied" });

        var config = await _configRepository.GetByOrgAsync(
            _currentOrg.OrganizationId.Value, cancellationToken);

        if (config is null)
            return Ok(new { domains = Array.Empty<object>(), message = "No email provider configured." });

        try
        {
            var domains = await _emailProviderFactory.ListDomainsAsync(
                _currentOrg.OrganizationId.Value, cancellationToken);

            return Ok(new
            {
                providerType = config.ProviderType,
                domains = domains.Select(d => new
                {
                    id = d.Id,
                    domain = d.Domain,
                    status = d.Status,
                    verifiedAt = d.VerifiedAt,
                    deliverabilityReady = d.DeliverabilityReady,
                })
            });
        }
        catch (Exception ex)
        {
            return Ok(new { domains = Array.Empty<object>(), error = $"Failed to fetch domains: {ex.Message}" });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Configure(
        [FromBody] ConfigureEmailProviderRequest request,
        CancellationToken cancellationToken)
    {
        if (_currentOrg.OrganizationId is null)
            return Unauthorized(new { error = "No organization context." });

        if (_currentOrg.Role == "member" || _currentOrg.Role == "revoked")
            return StatusCode(403, new { error = "permission_denied" });

        if (string.IsNullOrWhiteSpace(request.ApiKey))
            return BadRequest(new { error = "API key is required." });

        if (string.IsNullOrWhiteSpace(request.ProviderType))
            return BadRequest(new { error = "Provider type is required." });

        var supportedProviders = new[] { "resend", "sendbyte", "sendgrid", "brevo", "smtp" };
        if (!supportedProviders.Contains(request.ProviderType.ToLowerInvariant()))
            return BadRequest(new { error = $"Unsupported provider. Supported: {string.Join(", ", supportedProviders)}" });

        var encryptedKey = _encryptionService.Encrypt(request.ApiKey.Trim());

        var existing = await _configRepository.GetByOrgAsync(
            _currentOrg.OrganizationId.Value, cancellationToken);

        if (existing is not null)
        {
            existing.EncryptedApiKey = encryptedKey;
            existing.ProviderType = request.ProviderType.ToLowerInvariant();
            existing.SenderEmail = request.SenderEmail?.Trim();
            existing.IsActive = true;
            await _configRepository.SaveChangesAsync(cancellationToken);
        }
        else
        {
            var config = new EmailProviderConfig
            {
                OrganizationId = _currentOrg.OrganizationId.Value,
                ProviderType = request.ProviderType.ToLowerInvariant(),
                EncryptedApiKey = encryptedKey,
                SenderEmail = request.SenderEmail?.Trim(),
                IsActive = true,
            };
            await _configRepository.AddAsync(config, cancellationToken);
        }

        return Ok(new { configured = true, providerType = request.ProviderType.ToLowerInvariant() });
    }

    [HttpDelete]
    public async Task<IActionResult> RemoveConfig(CancellationToken cancellationToken)
    {
        if (_currentOrg.OrganizationId is null)
            return Unauthorized(new { error = "No organization context." });

        if (_currentOrg.Role == "member" || _currentOrg.Role == "revoked")
            return StatusCode(403, new { error = "permission_denied" });

        var config = await _configRepository.GetByOrgAsync(
            _currentOrg.OrganizationId.Value, cancellationToken);

        if (config is null)
            return NotFound(new { error = "No email provider configured." });

        config.IsActive = false;
        await _configRepository.SaveChangesAsync(cancellationToken);

        return Ok(new { removed = true });
    }
}

public record ConfigureEmailProviderRequest(
    string ProviderType,
    string ApiKey,
    string? SenderEmail = null
);
