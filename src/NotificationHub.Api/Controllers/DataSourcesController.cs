using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NotificationHub.Application.Abstractions;
using NotificationHub.Domain.Enums;
using NotificationHub.Shared.Abstractions;

namespace NotificationHub.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/datasources")]
[Authorize]
public class DataSourcesController : ControllerBase
{
    private readonly IDataSourceService _dataSourceService;
    private readonly IDataSourceRepository _dataSourceRepository;
    private readonly ICurrentOrganization _currentOrg;
    private readonly ICurrentUser _currentUser;

    public DataSourcesController(
        IDataSourceService dataSourceService,
        IDataSourceRepository dataSourceRepository,
        ICurrentOrganization currentOrg,
        ICurrentUser currentUser)
    {
        _dataSourceService = dataSourceService;
        _dataSourceRepository = dataSourceRepository;
        _currentOrg = currentOrg;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (_currentOrg.OrganizationId is null)
            return Unauthorized(new { error = "No organization context." });

        var (items, totalCount) = await _dataSourceRepository.GetPagedAsync(
            _currentOrg.OrganizationId.Value, page, pageSize, cancellationToken);

        return Ok(new
        {
            items = items.Select(ToResponse),
            totalCount, pageNumber = page, pageSize,
        });
    }

[HttpGet("{id:guid}/tables")]
public async Task<IActionResult> GetTables(Guid id, CancellationToken cancellationToken)
{
    if (_currentOrg.OrganizationId is null)
        return Unauthorized(new { error = "No organization context." });

    try
    {
        var tables = await _dataSourceService.GetTablesAsync(
            id, _currentOrg.OrganizationId.Value, cancellationToken);
        return Ok(new { tables });
    }
    catch (InvalidOperationException ex)
    {
        return BadRequest(new { error = ex.Message });
    }
}
    [HttpGet("{id:guid}/tables/{tableName}/columns")]
    public async Task<IActionResult> GetColumns(Guid id, string tableName, CancellationToken cancellationToken)
    {
        if (_currentOrg.OrganizationId is null)
            return Unauthorized(new { error = "No organization context." });

        try
        {
            var columns = await _dataSourceService.GetColumnsAsync(
                id, _currentOrg.OrganizationId.Value, tableName, cancellationToken);
            return Ok(new { tableName, columns });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
    
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        if (_currentOrg.OrganizationId is null)
            return Unauthorized(new { error = "No organization context." });

        var dataSource = await _dataSourceRepository.GetByIdAsync(
            id, _currentOrg.OrganizationId.Value, cancellationToken);

        if (dataSource is null)
            return NotFound(new { error = "Data source not found." });

        return Ok(ToResponse(dataSource));
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateDataSourceRequest request,
        CancellationToken cancellationToken)
    {
        if (_currentOrg.OrganizationId is null)
            return Unauthorized(new { error = "No organization context." });

        if (string.IsNullOrWhiteSpace(request.ConnectionString))
            return BadRequest(new { error = "ConnectionString is required." });

        try
        {
            var dataSource = await _dataSourceService.CreateAsync(new(
                _currentOrg.OrganizationId.Value,
                _currentUser.UserId!.Value,
                request.Name,
                request.Type,
                request.ConnectionString,
                request.Host,
                request.Database
            ), cancellationToken);

            return Ok(ToResponse(dataSource));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    // Never project EncryptedConnectionString — it should never leave this layer.
    private static object ToResponse(NotificationHub.Domain.Entities.DataSource d) => new
    {
        d.Id,
        d.Name,
        Type = d.Type.ToString(),
        d.Host,
        d.Database,
        Status = d.Status.ToString(),
        d.LastTestedAt,
        d.LastError,
        d.CreatedAt,
    };
}

public record CreateDataSourceRequest(
    string Name, DataSourceType Type, string ConnectionString, string? Host, string? Database);