export type DataSourceType =
  | 'PostgreSql' | 'MySql' | 'SqlServer'
  | 'Neon' | 'Supabase' | 'PlanetScale'
  | 'Csv' | 'MongoDb' | 'Airtable' | 'GoogleSheets'

// Only these are wired up for import today (ToSqlProtocol() on the backend
// returns null for the rest — Csv/MongoDb/Airtable/GoogleSheets need a
// different adapter shape that doesn't exist yet).
export const SQL_CAPABLE_TYPES: DataSourceType[] = [
  'PostgreSql', 'MySql', 'SqlServer', 'Neon', 'Supabase', 'PlanetScale',
]

export interface DataSource {
  id: string
  name: string
  type: DataSourceType
  host: string | null
  database: string | null
  status: string
  lastTestedAt: string | null
  lastError: string | null
  createdAt: string
}

export interface CreateDataSourcePayload {
  name: string
  type: DataSourceType
  connectionString: string
  host?: string
  database?: string
}

export interface PaginatedDataSources {
  items: DataSource[]
  totalCount: number
  pageNumber: number
  pageSize: number
}