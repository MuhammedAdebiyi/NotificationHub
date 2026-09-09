import { apiClient } from '@/shared/services/apiClient'
import type { Notification, NotificationDetail } from '../types/notification.types'

interface PaginatedNotifications {
  items: Notification[]
  totalCount: number
  pageNumber: number
  pageSize: number
}

export const notificationApi = {
  getAll: (page = 1, pageSize = 50, dateFrom?: string, dateTo?: string, status?: string) => {
    const params = new URLSearchParams({
      page: page.toString(),
      pageSize: pageSize.toString(),
    })
    if (dateFrom) params.set('dateFrom', dateFrom)
    if (dateTo) params.set('dateTo', dateTo)
    if (status) params.set('status', status)
    return apiClient.get<PaginatedNotifications>(
      `/api/v1/notifications?${params.toString()}`
    )
  },

  getById: (publicId: string) =>
    apiClient.get<NotificationDetail>(`/api/v1/notifications/${publicId}`),

  retry: (publicId: string) =>
    apiClient.post<{ success: boolean }>(`/api/v1/notifications/${publicId}/retry`),
}
