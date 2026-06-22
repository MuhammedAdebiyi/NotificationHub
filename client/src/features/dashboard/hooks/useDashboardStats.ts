import { useState, useEffect } from 'react'
import { dashboardApi } from '../api/dashboardApi'
import type { DashboardStats } from '../types/dashboard.types'

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
  const [isLoading, setIsLoading] = useState(true)

  useEffect(() => {
    dashboardApi
      .getStats()
      .then(setStats)
      .catch(() => setStats(FALLBACK_STATS)) // backend not built yet — fail quietly
      .finally(() => setIsLoading(false))
  }, [])

  return { stats, isLoading }
}