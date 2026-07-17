using NotificationHub.Domain.Common;
using NotificationHub.Domain.Enums;

namespace NotificationHub.Domain.Entities;

public class DataSource : BaseEntity
{
    public Guid OrganizationId { get; set; }
    public Guid? CreatedByUserId { get; set; }

    public string Name { get; set; } = string.Empty;
    public DataSourceType Type { get; set; }

    public string? Host { get; set; }
    public string? Database { get; set; }

    // AES-encrypted, decrypted only inside the Import Worker. Never returned by the API.
    public string EncryptedConnectionString { get; set; } = string.Empty;

    public DataSourceStatus Status { get; private set; } = DataSourceStatus.Pending;
    public DateTime? LastTestedAt { get; private set; }
    public string? LastError { get; private set; }

    public Organization? Organization { get; set; }
    public ICollection<ImportJob> ImportJobs { get; set; } = new List<ImportJob>();

    public void MarkTesting()
    {
        Status = DataSourceStatus.Testing;
    }

    public void MarkConnected()
    {
        Status = DataSourceStatus.Connected;
        LastTestedAt = DateTime.UtcNow;
        LastError = null;
    }

    public void MarkFailed(string error)
    {
        Status = DataSourceStatus.Failed;
        LastTestedAt = DateTime.UtcNow;
        LastError = error;
    }
}