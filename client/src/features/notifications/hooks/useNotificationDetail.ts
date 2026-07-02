import { useState, useEffect } from 'react'
import { notificationApi } from '../api/notificationApi'
import type { NotificationDetail } from '../types/notification.types'

export function useNotificationDetail(id: string) {
  const [notification, setNotification] = useState<NotificationDetail | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    notificationApi
      .getById(id)
      .then(setNotification)
      .catch((err) => setError(err.message))
      .finally(() => setIsLoading(false))
  }, [id])

  return { notification, isLoading, error }
}