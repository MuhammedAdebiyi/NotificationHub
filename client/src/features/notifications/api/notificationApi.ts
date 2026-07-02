import { apiClient } from '@/shared/services/apiClient'
import type { Notification, NotificationDetail } from '../types/notification.types'

interface PaginatedNotifications {
  items: Notification[]
  totalCount: number
  pageNumber: number
  pageSize: number
}

export const notificationApi = {
  getAll: (page = 1, pageSize = 20) =>
    apiClient.get<PaginatedNotifications>(
      `/api/v1/notifications?page=${page}&pageSize=${pageSize}`
    ),

  getById: (publicId: string) =>
    apiClient.get<NotificationDetail>(`/api/v1/notifications/${publicId}`),

  retry: (publicId: string) =>
    apiClient.post<{ success: boolean }>(`/api/v1/notifications/${publicId}/retry`),
}