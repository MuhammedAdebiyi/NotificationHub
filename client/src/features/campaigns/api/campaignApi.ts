import { apiClient } from '@/shared/services/apiClient'
import type { Campaign, CampaignDetail, CreateCampaignPayload } from '../types/campaign.types'

interface PaginatedCampaigns {
  items: Campaign[]
  totalCount: number
  pageNumber: number
  pageSize: number
}

export const campaignApi = {
  getAll: (page = 1, pageSize = 20) =>
    apiClient.get<PaginatedCampaigns>(`/api/v1/campaigns?page=${page}&pageSize=${pageSize}`),

  getById: (id: string) =>
    apiClient.get<CampaignDetail>(`/api/v1/campaigns/${id}`),

  create: (payload: CreateCampaignPayload) =>
    apiClient.post<{ id: string; title: string; status: string }>('/api/v1/campaigns', payload),

  addRecipients: (id: string, emails: string[]) =>
    apiClient.post<{ added: number; skipped: number }>(`/api/v1/campaigns/${id}/recipients`, { emails }),

  send: (id: string) =>
    apiClient.post<{ started: boolean }>(`/api/v1/campaigns/${id}/send`, {}),

  schedule: (id: string, scheduledAt: string) =>
    apiClient.post<{ scheduled: boolean }>(`/api/v1/campaigns/${id}/schedule`, { scheduledAt }),

  pause: (id: string) =>
    apiClient.post<{ paused: boolean }>(`/api/v1/campaigns/${id}/pause`, {}),

  resume: (id: string) =>
    apiClient.post<{ resumed: boolean }>(`/api/v1/campaigns/${id}/resume`, {}),

  delete: (id: string) =>
    apiClient.delete<{ deleted: boolean }>(`/api/v1/campaigns/${id}`),
}