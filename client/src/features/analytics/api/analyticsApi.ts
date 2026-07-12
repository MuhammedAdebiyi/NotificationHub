import { apiClient } from '@/shared/services/apiClient'
import type {
  OverviewStats,
  TimelinePoint,
  DeliveryFunnel,
  QueueSnapshot,
  CampaignAnalytics,
  FailureDto,
} from '../types/analytics.types'

export const analyticsApi = {
  getOverview: () =>
    apiClient.get<OverviewStats>('/api/v1/analytics/overview'),

  getTimeline: () =>
    apiClient.get<TimelinePoint[]>('/api/v1/analytics/timeline'),

  getDeliveryFunnel: () =>
    apiClient.get<DeliveryFunnel>('/api/v1/analytics/delivery-funnel'),

  getQueue: () =>
    apiClient.get<any>('/api/v1/analytics/queue').then(r => ({
      queueLength: r.pending ?? r.queueLength ?? 0,
      dlqLength: r.deadLetter ?? r.dlqLength ?? 0,
      processingRate: r.throughputPerMin ?? r.processingRate ?? 0,
      avgProcessingMs: r.avgWaitMs ?? r.avgProcessingMs ?? 0,
    } as QueueSnapshot)),

  getCampaigns: () =>
    apiClient.get<CampaignAnalytics>('/api/v1/analytics/campaigns'),

  getFailures: () =>
    apiClient.get<FailureDto[]>('/api/v1/analytics/failures'),
}