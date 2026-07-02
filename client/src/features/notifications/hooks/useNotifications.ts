import { useState, useEffect } from 'react'
import { notificationApi } from '../api/notificationApi'
import type { Notification } from '../types/notification.types'

export function useNotifications(page = 1, pageSize = 20) {
  const [notifications, setNotifications] = useState<Notification[]>([])
  const [totalCount, setTotalCount] = useState(0)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    notificationApi
      .getAll(page, pageSize)
      .then((res) => {
        setNotifications(res.items)
        setTotalCount(res.totalCount)
      })
      .catch((err) => setError(err.message))
      .finally(() => setIsLoading(false))
  }, [page, pageSize])

  return { notifications, totalCount, isLoading, error }
}