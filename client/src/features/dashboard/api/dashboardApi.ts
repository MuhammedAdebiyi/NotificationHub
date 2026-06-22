import { apiClient } from '@/shared/services/apiClient'
import type { DashboardStats, ActivityItem } from '../types/dashboard.types'

export const dashboardApi = {
  getStats: () =>
    apiClient.get<DashboardStats>('/api/dashboard/stats'),

  getRecentActivity: () =>
    apiClient.get<ActivityItem[]>('/api/dashboard/activity'),
}