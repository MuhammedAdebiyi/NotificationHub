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
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        if (_currentOrg.OrganizationId is null)
            return Unauthorized(new { error = "No organization context." });

        if (_currentOrg.Role == "member" || _currentOrg.Role == "revoked")
            return StatusCode(403, new { error = "permission_denied" });

        var providers = await _emailProviderFactory.GetAllProvidersAsync(
            _currentOrg.OrganizationId.Value, cancellationToken);

        return Ok(new
        {
            providers = providers.Select(p => new
            {
                id = p.Id,
                providerType = p.ProviderType,
                isActive = p.IsActive,
                isDefault = p.IsDefault,
                createdAt = p.CreatedAt,
            })
        });
    }

    [HttpGet("domains")]
    public async Task<IActionResult> ListDomains(CancellationToken cancellationToken)
    {
        if (_currentOrg.OrganizationId is null)
            return Unauthorized(new { error = "No organization context." });

        if (_currentOrg.Role == "member" || _currentOrg.Role == "revoked")
            return StatusCode(403, new { error = "permission_denied" });

        try
        {
            var domains = await _emailProviderFactory.ListDomainsAsync(
                _currentOrg.OrganizationId.Value, cancellationToken);

            return Ok(new
            {
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

        var orgId = _currentOrg.OrganizationId.Value;
        var providerType = request.ProviderType.ToLowerInvariant();
        var encryptedKey = _encryptionService.Encrypt(request.ApiKey.Trim());

        // Check if this provider type already exists for this org
        var existing = await _configRepository.GetByOrgAndTypeAsync(orgId, providerType, cancellationToken);

        if (existing is not null)
        {
            existing.EncryptedApiKey = encryptedKey;
            existing.SenderEmail = request.SenderEmail?.Trim();
            existing.IsActive = true;
            await _configRepository.SaveChangesAsync(cancellationToken);
            return Ok(new { configured = true, providerType, id = existing.Id });
        }

        // Check if this is the first provider — make it default automatically
        var allConfigs = await _configRepository.GetAllByOrgAsync(orgId, cancellationToken);
        var isFirst = allConfigs.Count == 0;

        var config = new EmailProviderConfig
        {
            OrganizationId = orgId,
            ProviderType = providerType,
            EncryptedApiKey = encryptedKey,
            SenderEmail = request.SenderEmail?.Trim(),
            IsActive = true,
            IsDefault = isFirst,
        };
        await _configRepository.AddAsync(config, cancellationToken);

        return Ok(new { configured = true, providerType, id = config.Id, isDefault = isFirst });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Remove(Guid id, CancellationToken cancellationToken)
    {
        if (_currentOrg.OrganizationId is null)
            return Unauthorized(new { error = "No organization context." });

        if (_currentOrg.Role == "member" || _currentOrg.Role == "revoked")
            return StatusCode(403, new { error = "permission_denied" });

        var configs = await _configRepository.GetAllByOrgAsync(
            _currentOrg.OrganizationId.Value, cancellationToken);

        var config = configs.FirstOrDefault(c => c.Id == id);
        if (config is null)
            return NotFound(new { error = "Provider not found." });

        config.IsActive = false;
        await _configRepository.SaveChangesAsync(cancellationToken);

        return Ok(new { removed = true });
    }

    [HttpPut("{id:guid}/default")]
    public async Task<IActionResult> SetDefault(Guid id, CancellationToken cancellationToken)
    {
        if (_currentOrg.OrganizationId is null)
            return Unauthorized(new { error = "No organization context." });

        if (_currentOrg.Role == "member" || _currentOrg.Role == "revoked")
            return StatusCode(403, new { error = "permission_denied" });

        var orgId = _currentOrg.OrganizationId.Value;
        var configs = await _configRepository.GetAllByOrgAsync(orgId, cancellationToken);

        var target = configs.FirstOrDefault(c => c.Id == id);
        if (target is null)
            return NotFound(new { error = "Provider not found." });

        // Unset current default and set new one
        foreach (var c in configs.Where(c => c.IsDefault))
            c.IsDefault = false;

        target.IsDefault = true;
        await _configRepository.SaveChangesAsync(cancellationToken);

        return Ok(new { updated = true, defaultProviderType = target.ProviderType });
    }
}

public record ConfigureEmailProviderRequest(
    string ProviderType,
    string ApiKey,
    string? SenderEmail = null
);
