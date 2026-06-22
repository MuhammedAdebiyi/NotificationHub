import { apiClient } from '@/shared/services/apiClient'
import type { Notification, NotificationDetail } from '../types/notification.types'
import type { PaginatedResponse } from '@/shared/types/api.types'

export const notificationApi = {
  getAll: (page = 1, pageSize = 20) =>
    apiClient.get<PaginatedResponse<Notification>>(`/api/notifications?page=${page}&pageSize=${pageSize}`),

  getById: (id: string) =>
    apiClient.get<NotificationDetail>(`/api/notifications/${id}`),

  retry: (id: string) =>
    apiClient.post<{ success: boolean }>(`/api/notifications/${id}/retry`),
}