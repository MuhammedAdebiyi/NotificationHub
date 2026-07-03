using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NotificationHub.Domain.Entities;
using NotificationHub.Infrastructure.Persistence;

namespace NotificationHub.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/templates")]
[Authorize]
public class TemplatesController : ControllerBase
{
    private readonly AppDbContext _context;

    public TemplatesController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Templates
            .Where(t => t.DeletedAt == null)
            .OrderByDescending(t => t.CreatedAt);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new { t.Id, t.Name, t.Subject, t.CreatedAt })
            .ToListAsync(cancellationToken);

        return Ok(new { items, totalCount = total, pageNumber = page, pageSize });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var template = await _context.Templates
            .FirstOrDefaultAsync(t => t.Id == id && t.DeletedAt == null, cancellationToken);

        if (template is null)
            return NotFound(new { error = "Template not found." });

        return Ok(new { template.Id, template.Name, template.Subject, template.Body, template.CreatedAt });
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] TemplateRequest request,
        CancellationToken cancellationToken)
    {
        var template = new Template
        {
            Name = request.Name,
            Subject = request.Subject,
            Body = request.Body,
        };

        await _context.Templates.AddAsync(template, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new { template.Id, template.Name });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] TemplateRequest request,
        CancellationToken cancellationToken)
    {
        var template = await _context.Templates
            .FirstOrDefaultAsync(t => t.Id == id && t.DeletedAt == null, cancellationToken);

        if (template is null)
            return NotFound(new { error = "Template not found." });

        template.Name = request.Name;
        template.Subject = request.Subject;
        template.Body = request.Body;
        template.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return Ok(new { template.Id, template.Name });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var template = await _context.Templates
            .FirstOrDefaultAsync(t => t.Id == id && t.DeletedAt == null, cancellationToken);

        if (template is null)
            return NotFound(new { error = "Template not found." });

        template.DeletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(new { deleted = true });
    }
}

public record TemplateRequest(string Name, string Subject, string Body);