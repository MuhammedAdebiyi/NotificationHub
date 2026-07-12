import { apiClient } from '@/shared/services/apiClient'
import type {
  OverviewStats,
  TimelinePoint,
  DeliveryFunnel,
  QueueSnapshot,
  CampaignAnalytics,
  FailureDto,
  AnalyticsHealth,
  InfrastructureHealth,
} from '../types/analytics.types'

export const analyticsApi = {
  
  getOverview: () =>
    apiClient.get<OverviewStats>('/api/v1/analytics/overview'),

  
  getTimeline: () =>
    apiClient.get<TimelinePoint[]>('/api/v1/analytics/timeline'),


  getDeliveryFunnel: () =>
    apiClient.get<DeliveryFunnel>('/api/v1/analytics/delivery-funnel'),

  getQueue: () =>
    apiClient.get<QueueSnapshot>('/api/v1/analytics/queue'),

  
  getCampaigns: () =>
    apiClient.get<CampaignAnalytics>('/api/v1/analytics/campaigns'),

  
  getFailures: () =>
    apiClient.get<FailureDto[]>('/api/v1/analytics/failures'),

  
  getHealth: () =>
    apiClient.get<AnalyticsHealth>('/api/v1/analytics/health'),

 
  getInfrastructure: () =>
    apiClient.get<InfrastructureHealth>('/api/v1/analytics/infrastructure'),
}