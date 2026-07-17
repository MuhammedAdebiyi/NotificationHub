using NotificationHub.Domain.Enums;

namespace NotificationHub.Application.Abstractions;

public record ConnectionTestResult(bool Success, string? Message);

public interface IConnectionTestService
{
    Task<ConnectionTestResult> TestConnectionAsync(
        Guid dataSourceId,
        DataSourceType type,
        string connectionString,
        CancellationToken cancellationToken = default);
}