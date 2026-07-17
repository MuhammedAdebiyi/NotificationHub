export type ImportJobStatus = 'Pending' | 'Running' | 'Completed' | 'Failed'

export interface ImportJob {
  id: string
  campaignId: string
  dataSourceId: string
  tableName: string
  status: ImportJobStatus
  rowsRead: number
  recipientsAdded: number
  errorCount: number
  lastError: string | null
  startedAt: string | null
  completedAt: string | null
  createdAt: string
}

export interface CreateImportJobPayload {
  dataSourceId: string
  tableName: string
  primaryKeyColumn: string
  emailColumn: string
  firstNameColumn?: string
  lastNameColumn?: string
  whereClause?: string
}