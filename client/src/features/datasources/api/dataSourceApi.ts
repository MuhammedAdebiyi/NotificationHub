import { apiClient } from '@/shared/services/apiClient'
import type {
  DataSource,
  ColumnInfo,
  CreateDataSourcePayload,
  PaginatedDataSources,
} from '../types/dataSource.types'

export const dataSourceApi = {
  getAll: (page = 1, pageSize = 20) =>
    apiClient.get<PaginatedDataSources>(`/api/v1/datasources?page=${page}&pageSize=${pageSize}`),

  getById: (id: string) =>
    apiClient.get<DataSource>(`/api/v1/datasources/${id}`),

  create: (payload: CreateDataSourcePayload) =>
    apiClient.post<DataSource>('/api/v1/datasources', payload),

  getTables: (id: string) =>
    apiClient.get<{ tables: string[] }>(`/api/v1/datasources/${id}/tables`),

  getColumns: (id: string, tableName: string) =>
    apiClient.get<{ tableName: string; columns: ColumnInfo[] }>(
      `/api/v1/datasources/${id}/tables/${encodeURIComponent(tableName)}/columns`
    ),

  delete: (id: string) =>
    apiClient.delete<{ deleted: boolean }>(`/api/v1/datasources/${id}`),
}