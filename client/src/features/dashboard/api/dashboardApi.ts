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

const BASE = '/api/v1'

async function get<T>(path: string): Promise<T> {
  const res = await fetch(`${BASE}${path}`, { credentials: 'include' })
  if (!res.ok) throw new Error(`${res.status} ${res.statusText}`)
  return res.json()
}

export const dashboardApi = {
  getHealth:           () => get<HealthBannerData>('/analytics/health'),
  getTodayStats:       () => get<TodayStats>('/analytics/overview'),
  getDeliveryTimeline: () => get<DeliveryPoint[]>('/analytics/timeline'),
  getQueue:            () => get<QueueStats>('/analytics/queue'),
  getCampaignSnapshot: () => get<CampaignSnapshot>('/analytics/campaigns'),
  getRecentFailures:   () => get<RecentFailure[]>('/analytics/failures'),
  getActivity:         () => get<ActivityItem[]>('/analytics/activity'),
  getInfrastructure:   () => get<InfrastructureHealth>('/analytics/system'),
  getOrgInfo:          () => get<{ name: string }>('/org/info'),
}