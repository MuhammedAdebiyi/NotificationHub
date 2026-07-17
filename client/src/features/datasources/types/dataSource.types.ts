export type DataSourceType =
  | 'PostgreSql' | 'MySql' | 'SqlServer'
  | 'Neon' | 'Supabase' | 'PlanetScale'
  | 'Csv' | 'MongoDb' | 'Airtable' | 'GoogleSheets'

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

export interface ColumnInfo {
  name: string
  dataType: string
  isNullable: boolean
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