using NotificationHub.Application.Abstractions;
using NotificationHub.Domain.Entities;
using NotificationHub.Infrastructure.Connections;

namespace NotificationHub.Infrastructure.Services;

public class DataSourceService : IDataSourceService
{
    private readonly IDataSourceRepository _repository;
    private readonly IConnectionTestService _connectionTestService;
    private readonly IEncryptionService _encryptionService;
    private readonly ISchemaInspectionService _schemaInspectionService;

    public DataSourceService(
        IDataSourceRepository repository,
        IConnectionTestService connectionTestService,
        IEncryptionService encryptionService,
        ISchemaInspectionService schemaInspectionService)
    {
        _repository = repository;
        _connectionTestService = connectionTestService;
        _encryptionService = encryptionService;
        _schemaInspectionService = schemaInspectionService;
    }

    public async Task<DataSource> CreateAsync(
        CreateDataSourceCommand command, CancellationToken cancellationToken = default)
    {
        if (command.Type.ToSqlProtocol() is null)
        {
            throw new InvalidOperationException(
                $"{command.Type} is not yet supported for connection-string based import.");
        }

        var dataSource = new DataSource
        {
            OrganizationId = command.OrganizationId,
            CreatedByUserId = command.UserId,
            Name = command.Name,
            Type = command.Type,
            Host = command.Host,
            Database = command.Database,
        };

        dataSource.MarkTesting();

        var testResult = await _connectionTestService.TestConnectionAsync(
            dataSource.Id, command.Type, command.ConnectionString, cancellationToken);

        dataSource.EncryptedConnectionString = _encryptionService.Encrypt(command.ConnectionString);

        if (testResult.Success)
            dataSource.MarkConnected();
        else
            dataSource.MarkFailed(testResult.Message ?? "Connection test failed.");

        return await _repository.AddAsync(dataSource, cancellationToken);
    }

    public async Task<List<string>> GetTablesAsync(
        Guid dataSourceId, Guid organizationId, CancellationToken cancellationToken = default)
    {
        var dataSource = await GetOwnedDataSourceOrThrow(dataSourceId, organizationId, cancellationToken);
        var connectionString = _encryptionService.Decrypt(dataSource.EncryptedConnectionString);
        return await _schemaInspectionService.GetTablesAsync(dataSource.Type, connectionString, cancellationToken);
    }

    public async Task<List<ColumnInfo>> GetColumnsAsync(
        Guid dataSourceId, Guid organizationId, string tableName, CancellationToken cancellationToken = default)
    {
        var dataSource = await GetOwnedDataSourceOrThrow(dataSourceId, organizationId, cancellationToken);
        var connectionString = _encryptionService.Decrypt(dataSource.EncryptedConnectionString);
        return await _schemaInspectionService.GetColumnsAsync(dataSource.Type, connectionString, tableName, cancellationToken);
    }

    private async Task<DataSource> GetOwnedDataSourceOrThrow(
        Guid dataSourceId, Guid organizationId, CancellationToken cancellationToken)
    {
        var dataSource = await _repository.GetByIdAsync(dataSourceId, organizationId, cancellationToken);
        if (dataSource is null)
            throw new InvalidOperationException("Data source not found.");
        return dataSource;
    }
}