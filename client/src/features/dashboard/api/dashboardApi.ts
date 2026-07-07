import { apiClient } from '@/shared/services/apiClient'
import type { DashboardStats, ActivityItem } from '../types/dashboard.types'

export const dashboardApi = {
  getStats: () =>
    apiClient.get<DashboardStats>('/api/v1/dashboard/stats'),

  getActivity: () =>
    apiClient.get<ActivityItem[]>('/api/v1/dashboard/activity'),

  getVolume: () =>
    apiClient.get<{ date: string; count: number }[]>('/api/v1/dashboard/volume'),

  getOrgInfo: () =>
    apiClient.get<{ name: string; plan: string; slug: string }>('/api/v1/org/info'),
}