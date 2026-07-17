import { apiClient } from '@/shared/services/apiClient'
import type { ImportJob, CreateImportJobPayload } from '../types/import.types'

export const importApi = {
  create: (campaignId: string, payload: CreateImportJobPayload) =>
    apiClient.post<ImportJob>(`/api/v1/campaigns/${campaignId}/imports`, payload),

  getById: (campaignId: string, importJobId: string) =>
    apiClient.get<ImportJob>(`/api/v1/campaigns/${campaignId}/imports/${importJobId}`),
}