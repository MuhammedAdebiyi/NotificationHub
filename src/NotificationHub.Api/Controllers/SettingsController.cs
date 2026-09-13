using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NotificationHub.Application.Abstractions;
using NotificationHub.Domain.Entities;
using NotificationHub.Infrastructure.Auth;
using NotificationHub.Shared.Abstractions;

namespace NotificationHub.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/org/api-keys")]
[Authorize]
public class SettingsController : ControllerBase
{
    private readonly IApiKeyRepository _apiKeyRepository;
    private readonly ICurrentOrganization _currentOrg;
    private readonly ITokenGenerator _tokenGenerator;
    private readonly IClock _clock;

    public SettingsController(
        IApiKeyRepository apiKeyRepository,
        ICurrentOrganization currentOrg,
        ITokenGenerator tokenGenerator,
        IClock clock)
    {
        _apiKeyRepository = apiKeyRepository;
        _currentOrg = currentOrg;
        _tokenGenerator = tokenGenerator;
        _clock = clock;
    }

    [HttpGet]
    public async Task<IActionResult> GetKeys(CancellationToken cancellationToken)
    {
        if (_currentOrg.OrganizationId is null)
            return Unauthorized(new { error = "No organization context." });

        if (_currentOrg.Role == "member" || _currentOrg.Role == "revoked")
            return StatusCode(403, new { error = "permission_denied" });

        var keys = await _apiKeyRepository.GetByOrgAsync(
            _currentOrg.OrganizationId.Value, cancellationToken);

        return Ok(new
        {
            items = keys.Select(k => new
            {
                k.Id,
                k.Name,
                k.KeyPrefix,
                k.Environment,
                k.IsActive,
                k.CreatedAt,
                k.LastUsedAt,
            })
        });
    }

    [HttpPost]
    public async Task<IActionResult> CreateKey(
        [FromBody] CreateApiKeyRequest request,
        CancellationToken cancellationToken)
    {
        if (_currentOrg.OrganizationId is null)
            return Unauthorized(new { error = "No organization context." });

        if (_currentOrg.Role == "member" || _currentOrg.Role == "revoked")
            return StatusCode(403, new { error = "permission_denied" });

        var env = string.Equals(request.Environment, "test", StringComparison.OrdinalIgnoreCase) ? "test" : "production";
        var prefix = env == "test" ? "nhub_test_" : "nhub_live_";
        var plaintext = $"{prefix}{_tokenGenerator.Generate(32)}";
        var hash = ApiKeyGenerator.Hash(plaintext);

        var apiKey = new ApiKey
        {
            OrganizationId = _currentOrg.OrganizationId.Value,
            Name = request.Name.Trim(),
            KeyHash = hash,
            KeyPrefix = ApiKeyGenerator.GetPrefix(plaintext),
            Environment = env,
            IsActive = true,
        };

        await _apiKeyRepository.AddAsync(apiKey, cancellationToken);
        await _apiKeyRepository.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            key = plaintext,
            id = apiKey.Id,
            name = apiKey.Name,
            keyPrefix = apiKey.KeyPrefix,
            environment = apiKey.Environment,
            createdAt = apiKey.CreatedAt,
        });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> RevokeKey(Guid id, CancellationToken cancellationToken)
    {
        if (_currentOrg.OrganizationId is null)
            return Unauthorized(new { error = "No organization context." });

        if (_currentOrg.Role == "member" || _currentOrg.Role == "revoked")
            return StatusCode(403, new { error = "permission_denied" });

        var key = await _apiKeyRepository.GetByIdAsync(
            id, _currentOrg.OrganizationId.Value, cancellationToken);

        if (key is null)
            return NotFound(new { error = "API key not found." });

        key.IsActive = false;
        key.DeletedAt = _clock.UtcNow;
        await _apiKeyRepository.SaveChangesAsync(cancellationToken);

        return Ok(new { revoked = true });
    }
}

public record CreateApiKeyRequest(string Name, string? Environment = null);