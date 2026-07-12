import { apiClient } from '@/shared/services/apiClient'
import type {
  OverviewStats,
  TimelineStats,
  DeliveryFunnel,
  QueueSnapshot,
  CampaignAnalytics,
  FailureDto,
  ProviderStats,
  UsageStats,
} from '../types/analytics.types'

export const analyticsApi = {
  getOverview: () =>
    apiClient.get<OverviewStats>('/api/v1/analytics/overview'),

  getTimeline: () =>
    apiClient.get<TimelineStats>('/api/v1/analytics/timeline'),

  getDeliveryFunnel: () =>
    apiClient.get<DeliveryFunnel>('/api/v1/analytics/delivery-funnel'),

  getQueue: () =>
    apiClient.get<QueueSnapshot>('/api/v1/analytics/queue'),

  getCampaigns: () =>
    apiClient.get<CampaignAnalytics>('/api/v1/analytics/campaigns'),

  getFailures: () =>
    apiClient.get<FailureDto[]>('/api/v1/analytics/failures'),

  getProviders: () =>
    apiClient.get<ProviderStats>('/api/v1/analytics/providers'),

  getUsage: () =>
    apiClient.get<UsageStats>('/api/v1/analytics/usage'),
}