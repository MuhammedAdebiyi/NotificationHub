using NotificationHub.Domain.Common;
using NotificationHub.Domain.Enums;

namespace NotificationHub.Domain.Entities;

public class ImportJob : BaseEntity
{
    public Guid OrganizationId { get; set; }
    public Guid CampaignId { get; set; }
    public Guid DataSourceId { get; set; }
    public Guid? CreatedByUserId { get; set; }

    public string TableName { get; set; } = string.Empty;
    public string PrimaryKeyColumn { get; set; } = string.Empty;
    public string EmailColumn { get; set; } = string.Empty;
    public string? FirstNameColumn { get; set; }
    public string? LastNameColumn { get; set; }

    // Optional safe filter, e.g. "is_verified = true" — validated, never raw-concatenated SQL
    public string? WhereClause { get; set; }

    public ImportJobStatus Status { get; private set; } = ImportJobStatus.Pending;

    public DateTime? StartedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    public long RowsRead { get; private set; }
    public long RecipientsAdded { get; private set; }
    public int ErrorCount { get; private set; }
    public string? LastError { get; private set; }

    // Resume cursor for keyset pagination (last primary key value processed)
    public string? LastCursorId { get; private set; }

    public Organization? Organization { get; set; }
    public DataSource? DataSource { get; set; }
    public Campaign? Campaign { get; set; }

    public void Start()
    {
        Status = ImportJobStatus.Running;
        StartedAt = DateTime.UtcNow;
    }

    public void RecordBatch(long rowsRead, long recipientsAdded, string? lastCursorId)
    {
        RowsRead += rowsRead;
        RecipientsAdded += recipientsAdded;
        LastCursorId = lastCursorId;
    }

    public void RecordError(string error)
    {
        ErrorCount++;
        LastError = error;
    }

    public void Complete()
    {
        Status = ImportJobStatus.Completed;
        CompletedAt = DateTime.UtcNow;
    }

    public void Fail(string error)
    {
        Status = ImportJobStatus.Failed;
        CompletedAt = DateTime.UtcNow;
        LastError = error;
    }
}