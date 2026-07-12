import { useState, useEffect } from 'react'
import { analyticsApi } from '../api/analyticsApi'
import type {
  OverviewStats,
  TimelinePoint,
  DeliveryFunnel,
  QueueSnapshot,
  CampaignAnalytics,
  FailureDto,
} from '../types/analytics.types'

export function useAnalytics() {
  const [overview, setOverview] = useState<OverviewStats | null>(null)
  const [timeline, setTimeline] = useState<TimelinePoint[]>([])
  const [funnel, setFunnel] = useState<DeliveryFunnel | null>(null)
  const [queue, setQueue] = useState<QueueSnapshot | null>(null)
  const [campaigns, setCampaigns] = useState<CampaignAnalytics | null>(null)
  const [failures, setFailures] = useState<FailureDto[]>([])
  const [isLoading, setIsLoading] = useState(true)

  useEffect(() => {
    Promise.allSettled([
      analyticsApi.getOverview().then(setOverview),
      analyticsApi.getTimeline().then(setTimeline),
      analyticsApi.getDeliveryFunnel().then(setFunnel),
      analyticsApi.getQueue().then(setQueue),
      analyticsApi.getCampaigns().then(setCampaigns),
      analyticsApi.getFailures().then(setFailures),
    ]).finally(() => setIsLoading(false))
  }, [])

  return { overview, timeline, funnel, queue, campaigns, failures, isLoading }
}