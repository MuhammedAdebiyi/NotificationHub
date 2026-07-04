import { useState, useEffect } from 'react'
import { dashboardApi } from '../api/dashboardApi'
import type { DashboardStats, ActivityItem } from '../types/dashboard.types'

const FALLBACK_STATS: DashboardStats = {
  totalSent: 0,
  pending: 0,
  failed: 0,
  successRate: 0,
  queueLength: 0,
  activeUsers: 0,
}

export function useDashboardStats() {
  const [stats, setStats] = useState<DashboardStats>(FALLBACK_STATS)
  const [activity, setActivity] = useState<ActivityItem[]>([])
  const [isLoading, setIsLoading] = useState(true)

  useEffect(() => {
    dashboardApi
      .getStats()
      .then(setStats)
      .catch(() => setStats(FALLBACK_STATS))
      .finally(() => setIsLoading(false))

    dashboardApi
      .getActivity()
      .then(setActivity)
      .catch(() => setActivity([]))
  }, [])

  return { stats, activity, isLoading }
}