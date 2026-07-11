export type SystemStatus = 'healthy' | 'degraded' | 'down'

export interface HealthBannerData {
  status: 'healthy' | 'warning' | 'critical'
  successRate: number
  queueLatencyMs: number
  workersOnline: number
  incidentMessage?: string
}

export interface TodayStats {
  notificationsSent: number
  notificationsSentDelta: number
  successRate: number
  successRateDelta: number
  queueDepth: number
  workersActive: number
  avgSendTimeMs: number
  p95SendTimeMs: number
  deadLetters: number
  deadLettersNeedingReview: number
  campaignsRunning: number
  campaignsScheduled: number
  apiCallsToday: number
  plan: string
  planUsagePct: number
}

export interface DeliveryPoint {
  hour: string
  sent: number
  failed: number
  retrying: number
}

export interface QueueStats {
  pending: number
  processing: number
  retrying: number
  deadLetter: number
  avgWaitMs: number
  workersActive: number
  workerCapacity: number
  throughputPerMin: number
}

export interface CampaignSnapshot {
  running: number
  scheduled: number
  drafts: number
  completedToday: number
  recent: RecentCampaign[]
}

export interface RecentCampaign {
  id: string
  title: string
  recipientCount: number
  status: 'Running' | 'Completed' | 'Scheduled' | 'Draft'
  progressPercent?: number
  scheduledAt?: string
}

export interface RecentFailure {
  notificationId: string
  title: string
  reason: string
  failureType: 'smtp_timeout' | 'rate_limited' | 'invalid_email' | 'dead_letter' | 'unknown'
  status: 'retrying' | 'dead_letter' | 'failed'
  occurredAt: string
}

export interface ActivityItem {
  id: string
  time: string
  type: 'sent' | 'failed' | 'retry' | 'campaign' | 'key' | 'invite' | 'dlq'
  title: string
  subtitle: string
}

export interface ServiceHealth {
  name: string
  status: SystemStatus
  detail: string
}

export interface InfrastructureHealth {
  services: ServiceHealth[]
}