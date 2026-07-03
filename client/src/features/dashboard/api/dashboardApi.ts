import { apiClient } from '@/shared/services/apiClient'
import type { DashboardStats, ActivityItem } from '../types/dashboard.types'

export const dashboardApi = {
  getStats: () =>
    apiClient.get<DashboardStats>('/api/v1/dashboard/stats'),

  getRecentActivity: () =>
    apiClient.get<ActivityItem[]>('/api/v1/dashboard/activity'),
}