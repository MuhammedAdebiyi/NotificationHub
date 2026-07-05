using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NotificationHub.Application.Abstractions;
using NotificationHub.Domain.Entities;
using NotificationHub.Shared.Abstractions;

namespace NotificationHub.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/templates")]
[Authorize]
public class TemplatesController : ControllerBase
{
    private readonly ITemplateRepository _repository;
    private readonly ICurrentOrganization _currentOrg;

    public TemplatesController(ITemplateRepository repository, ICurrentOrganization currentOrg)
    {
        _repository = repository;
        _currentOrg = currentOrg;
    }

    [HttpGet]
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

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        if (_currentOrg.OrganizationId is null)
            return Unauthorized(new { error = "No organization context." });

        var template = await _repository.GetByIdAsync(id, _currentOrg.OrganizationId.Value, cancellationToken);

        if (template is null)
            return NotFound(new { error = "Template not found." });

        return Ok(new { template.Id, template.Name, template.Subject, template.Body, template.CreatedAt });
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] TemplateRequest request,
        CancellationToken cancellationToken)
    {
        if (_currentOrg.OrganizationId is null)
            return Unauthorized(new { error = "No organization context." });

        var template = new Template
        {
            OrganizationId = _currentOrg.OrganizationId.Value,
            Name = request.Name,
            Subject = request.Subject,
            Body = request.Body,
        };

        await _repository.AddAsync(template, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return Ok(new { template.Id, template.Name });
    }

    [HttpPut("{id:guid}")]
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

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        if (_currentOrg.OrganizationId is null)
            return Unauthorized(new { error = "No organization context." });

        await _repository.DeleteAsync(id, _currentOrg.OrganizationId.Value, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);
        return Ok(new { deleted = true });
    }
}

public record TemplateRequest(string Name, string Subject, string Body);