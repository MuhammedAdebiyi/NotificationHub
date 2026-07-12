export interface OverviewStats {
  notificationsSent: number
  notificationsSentDelta: number
  successRate: number
  successRateDelta: number
  queueDepth: number
  workersActive: number
  avgSendTimeMs: number
  p95SendTimeMs: number
  deadLetters: number
  campaignsRunning: number
  campaignsScheduled: number
  plan: string
  planUsagePct: number
}

export interface TimelinePoint {
  hour: string
  sent: number
  failed: number
  retrying: number
  queued: number
  processing: number
}

export interface DeliveryFunnel {
  queued: number
  processing: number
  sent: number
  failed: number
  deadLetter: number
}

export interface QueueSnapshot {
  queueLength: number
  dlqLength: number
  processingRate: number
  avgProcessingMs: number
}

export interface CampaignAnalytics {
  total: number
  completed: number
  running: number
  draft: number
  totalRecipients: number
  totalDelivered: number
  totalFailed: number
  deliveryRate: number
  topCampaigns: TopCampaign[]
}

export interface TopCampaign {
  id: string
  title: string
  status: string
  totalRecipients: number
  delivered: number
  failed: number
  deliveryRate: number
  createdAt: string
}

export interface FailureDto {
  publicId: string
  type: string
  retryCount: number
  createdAt: string
  provider: string
  errorMessage: string
}