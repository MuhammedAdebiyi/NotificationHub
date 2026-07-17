using NotificationHub.Domain.Enums;

namespace NotificationHub.Infrastructure.Connections;

public static class DataSourceTypeExtensions
{
    public static SqlProtocol? ToSqlProtocol(this DataSourceType type) => type switch
    {
        DataSourceType.PostgreSql => SqlProtocol.Postgres,
        DataSourceType.Neon => SqlProtocol.Postgres,
        DataSourceType.Supabase => SqlProtocol.Postgres,
        DataSourceType.MySql => SqlProtocol.MySql,
        DataSourceType.PlanetScale => SqlProtocol.MySql,
        DataSourceType.SqlServer => SqlProtocol.SqlServer,
        _ => null // Csv, MongoDb, Airtable, GoogleSheets — not SQL, no connection string protocol
    };
}