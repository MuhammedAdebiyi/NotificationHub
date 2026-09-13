using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NotificationHub.Application.Abstractions;
using NotificationHub.Domain.Entities;
using NotificationHub.Shared.Abstractions;

namespace NotificationHub.Api.Controllers;

/// <summary>
/// Create and manage reusable email templates with variable placeholders.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/templates")]
[Authorize]
public class TemplatesController : ControllerBase
{
    private readonly ITemplateRepository _repository;
    private readonly ICurrentOrganization _currentOrg;
    private readonly ICurrentUser _currentUser;

    public TemplatesController(ITemplateRepository repository, ICurrentOrganization currentOrg, ICurrentUser currentUser)
    {
        _repository = repository;
        _currentOrg = currentOrg;
        _currentUser = currentUser;
    }

    /// <summary>
    /// List all templates for your organization.
    /// </summary>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Items per page (default: 20)</param>
    /// <returns>Paginated list of templates</returns>
    [HttpGet]
    [ProducesResponseType(typeof(object), 200)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (_currentOrg.OrganizationId is null)
            return Unauthorized(new { error = "No organization context." });

        var (items, totalCount) = await _repository.GetPagedAsync(
            _currentOrg.OrganizationId.Value, page, pageSize, cancellationToken);

        return Ok(new
        {
            items = items.Select(t => new { t.Id, t.Name, t.Subject, t.CreatedAt }),
            totalCount,
            pageNumber = page,
            pageSize
        });
    }

    /// <summary>
    /// Get a template by ID, including its body content.
    /// </summary>
    /// <param name="id">Template ID</param>
    /// <returns>Full template detail</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        if (_currentOrg.OrganizationId is null)
            return Unauthorized(new { error = "No organization context." });

        var template = await _repository.GetByIdAsync(id, _currentOrg.OrganizationId.Value, cancellationToken);

        if (template is null)
            return NotFound(new { error = "Template not found." });

        return Ok(new { template.Id, template.Name, template.Subject, template.Body, template.CreatedAt });
    }

    /// <summary>
    /// Create a new email template.
    /// </summary>
    /// <param name="request">Template details (name, subject, body)</param>
    /// <returns>Created template ID and name</returns>
    [HttpPost]
    [ProducesResponseType(typeof(object), 200)]
    public async Task<IActionResult> Create(
        [FromBody] TemplateRequest request,
        CancellationToken cancellationToken)
    {
        if (_currentOrg.OrganizationId is null)
            return Unauthorized(new { error = "No organization context." });

        if (_currentUser.UserId is null)
            return Unauthorized(new { error = "No user context." });

        var template = new Template
        {
            OrganizationId = _currentOrg.OrganizationId.Value,
            CreatedByUserId = _currentUser.UserId.Value,
            Name = request.Name,
            Subject = request.Subject,
            Body = request.Body,
        };

        await _repository.AddAsync(template, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return Ok(new { template.Id, template.Name });
    }

    /// <summary>
    /// Update an existing template.
    /// </summary>
    /// <param name="id">Template ID</param>
    /// <param name="request">Updated template details</param>
    /// <returns>Updated template ID and name</returns>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] TemplateRequest request,
        CancellationToken cancellationToken)
    {
        if (_currentOrg.OrganizationId is null)
            return Unauthorized(new { error = "No organization context." });

        var template = await _repository.GetByIdAsync(id, _currentOrg.OrganizationId.Value, cancellationToken);

        if (template is null)
            return NotFound(new { error = "Template not found." });

        template.Name = request.Name;
        template.Subject = request.Subject;
        template.Body = request.Body;
        template.UpdatedAt = DateTime.UtcNow;

        await _repository.SaveChangesAsync(cancellationToken);
        return Ok(new { template.Id, template.Name });
    }

    /// <summary>
    /// Delete a template.
    /// </summary>
    /// <param name="id">Template ID</param>
    /// <returns>Deletion confirmation</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        if (_currentOrg.OrganizationId is null)
            return Unauthorized(new { error = "No organization context." });

        await _repository.DeleteAsync(id, _currentOrg.OrganizationId.Value, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);
        return Ok(new { deleted = true });
    }
}

/// <summary>
/// Request body for creating or updating a template.
/// </summary>
public record TemplateRequest(
    /// <summary>Template name (e.g. "Welcome Email")</summary>
    string Name,
    /// <summary>Email subject line (supports {{variable}} placeholders)</summary>
    string Subject,
    /// <summary>Email body HTML (supports {{variable}} placeholders)</summary>
    string Body
);
