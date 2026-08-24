import { useState, useEffect, useRef, useCallback } from 'react'
import { dashboardApi } from '../api/dashboardApi'
import type {
  HealthBannerData,
  TodayStats,
  DeliveryPoint,
  QueueStats,
  CampaignSnapshot,
  RecentFailure,
  ActivityItem,
  InfrastructureHealth,
} from '../types'

interface DashboardData {
  health: HealthBannerData | null
  stats: TodayStats | null
  timeline: DeliveryPoint[]
  queue: QueueStats | null
  campaigns: CampaignSnapshot | null
  failures: RecentFailure[]
  activity: ActivityItem[]
  infra: InfrastructureHealth | null
  orgInfo: { name: string; plan: string } | null
}

const EMPTY_TIMELINE: DeliveryPoint[] = []
const EMPTY_FAILURES: RecentFailure[] = []
const EMPTY_ACTIVITY: ActivityItem[] = []

export function useDashboardData() {
  const [data, setData] = useState<DashboardData>({
    health: null,
    stats: null,
    timeline: EMPTY_TIMELINE,
    queue: null,
    campaigns: null,
    failures: EMPTY_FAILURES,
    activity: EMPTY_ACTIVITY,
    infra: null,
    orgInfo: null,
  })

  const mountedRef = useRef(true)

  const fetchAll = useCallback(async () => {
    const results = await Promise.allSettled([
      dashboardApi.getHealth(),
      dashboardApi.getTodayStats(),
      dashboardApi.getDeliveryTimeline(),
      dashboardApi.getQueue(),
      dashboardApi.getCampaignSnapshot(),
      dashboardApi.getRecentFailures(),
      dashboardApi.getActivity(),
      dashboardApi.getInfrastructure(),
      dashboardApi.getOrgInfo(),
    ])

    if (!mountedRef.current) return

    setData(prev => ({
      health:       results[0].status === 'fulfilled' ? results[0].value : prev.health,
      stats:        results[1].status === 'fulfilled' ? results[1].value : prev.stats,
      timeline:     results[2].status === 'fulfilled' ? results[2].value : prev.timeline,
      queue:        results[3].status === 'fulfilled' ? results[3].value : prev.queue,
      campaigns:    results[4].status === 'fulfilled' ? results[4].value : prev.campaigns,
      failures:     results[5].status === 'fulfilled' ? results[5].value : prev.failures,
      activity:     results[6].status === 'fulfilled' ? results[6].value : prev.activity,
      infra:        results[7].status === 'fulfilled' ? results[7].value : prev.infra,
      orgInfo:      results[8].status === 'fulfilled' ? results[8].value : prev.orgInfo,
    }))
  }, [])

  useEffect(() => {
    mountedRef.current = true
    fetchAll()
    const timer = setInterval(fetchAll, 5000)
    return () => { mountedRef.current = false; clearInterval(timer) }
  }, [fetchAll])

  return data
}
