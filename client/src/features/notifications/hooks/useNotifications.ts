import { useState, useEffect } from 'react'
import { notificationApi } from '../api/notificationApi'
import type { Notification } from '../types/notification.types'

export function useNotifications() {
  const [notifications, setNotifications] = useState<Notification[]>([])
  const [totalCount, setTotalCount] = useState(0)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState(50)
  const [dateFrom, setDateFrom] = useState('')
  const [dateTo, setDateTo] = useState('')

  useEffect(() => {
    setIsLoading(true)
    notificationApi
      .getAll(page, pageSize, dateFrom || undefined, dateTo || undefined)
      .then((res) => {
        setNotifications(res.items)
        setTotalCount(res.totalCount)
      })
      .catch((err) => setError(err.message))
      .finally(() => setIsLoading(false))
  }, [page, pageSize, dateFrom, dateTo])

  return {
    notifications,
    totalCount,
    isLoading,
    error,
    page,
    setPage,
    pageSize,
    setPageSize,
    dateFrom,
    setDateFrom,
    dateTo,
    setDateTo,
  }
}
