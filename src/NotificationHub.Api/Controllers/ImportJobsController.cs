using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NotificationHub.Application.Abstractions;
using NotificationHub.Domain.Entities;
using NotificationHub.Domain.Enums;
using NotificationHub.Shared.Abstractions;
using NotificationHub.Infrastructure.Connections;

namespace NotificationHub.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/campaigns/{campaignId:guid}/imports")]
[Authorize]
public class ImportJobsController : ControllerBase
{
    private readonly IImportJobRepository _importJobRepository;
    private readonly ICampaignRepository _campaignRepository;
    private readonly IDataSourceRepository _dataSourceRepository;
    private readonly ICurrentOrganization _currentOrg;
    private readonly ICurrentUser _currentUser;

    public ImportJobsController(
        IImportJobRepository importJobRepository,
        ICampaignRepository campaignRepository,
        IDataSourceRepository dataSourceRepository,
        ICurrentOrganization currentOrg,
        ICurrentUser currentUser)
    {
        _importJobRepository = importJobRepository;
        _campaignRepository = campaignRepository;
        _dataSourceRepository = dataSourceRepository;
        _currentOrg = currentOrg;
        _currentUser = currentUser;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        Guid campaignId,
        [FromBody] CreateImportJobRequest request,
        CancellationToken cancellationToken)
    {
        if (_currentOrg.OrganizationId is null)
            return Unauthorized(new { error = "No organization context." });

        var orgId = _currentOrg.OrganizationId.Value;

        try
        {
            var campaign = await _campaignRepository.GetByIdAsync(campaignId, orgId, cancellationToken)
                ?? throw new InvalidOperationException("Campaign not found.");

            if (campaign.Status != CampaignStatus.Draft)
                throw new InvalidOperationException("Can only import recipients into a draft campaign.");

            var dataSource = await _dataSourceRepository.GetByIdAsync(request.DataSourceId, orgId, cancellationToken)
                ?? throw new InvalidOperationException("Data source not found.");

            if (dataSource.Type.ToSqlProtocol() is null)
                throw new InvalidOperationException(
                    $"Data source type '{dataSource.Type}' is not yet supported for import.");

            if (string.IsNullOrWhiteSpace(request.TableName))
                throw new InvalidOperationException("TableName is required.");
            if (string.IsNullOrWhiteSpace(request.PrimaryKeyColumn))
                throw new InvalidOperationException("PrimaryKeyColumn is required.");
            if (string.IsNullOrWhiteSpace(request.EmailColumn))
                throw new InvalidOperationException("EmailColumn is required.");

            var job = new ImportJob
            {
                OrganizationId = orgId,
                CampaignId = campaignId,
                DataSourceId = dataSource.Id,
                CreatedByUserId = _currentUser.UserId,
                TableName = request.TableName,
                PrimaryKeyColumn = request.PrimaryKeyColumn,
                EmailColumn = request.EmailColumn,
                FirstNameColumn = request.FirstNameColumn,
                LastNameColumn = request.LastNameColumn,
                WhereClause = request.WhereClause,
                // Status defaults to Pending in the entity — do not set here (private setter).
            };

            await _importJobRepository.AddAsync(job, cancellationToken);
            await _importJobRepository.SaveChangesAsync(cancellationToken);

            return Ok(ToResponse(job));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid campaignId, Guid id, CancellationToken cancellationToken)
    {
        if (_currentOrg.OrganizationId is null)
            return Unauthorized(new { error = "No organization context." });

        var job = await _importJobRepository.GetByIdAsync(id, _currentOrg.OrganizationId.Value, cancellationToken);

        if (job is null || job.CampaignId != campaignId)
            return NotFound(new { error = "Import job not found." });

        return Ok(ToResponse(job));
    }

    private static object ToResponse(ImportJob job) => new
    {
        job.Id,
        job.CampaignId,
        job.DataSourceId,
        job.TableName,
        Status = job.Status.ToString(),
        job.RowsRead,
        job.RecipientsAdded,
        job.ErrorCount,
        job.LastError,
        job.StartedAt,
        job.CompletedAt,
        job.CreatedAt,
    };
}

public record CreateImportJobRequest(
    Guid DataSourceId,
    string TableName,
    string PrimaryKeyColumn,
    string EmailColumn,
    string? FirstNameColumn,
    string? LastNameColumn,
    string? WhereClause);