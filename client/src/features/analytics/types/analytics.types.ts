export interface OverviewStats {
  totalSent: number
  totalFailed: number
  totalPending: number
  successRate: number
  thisMonth: number
  lastMonth: number
  monthGrowth: number
  activeUsers: number
}

export interface TimelinePoint {
  date: string
  sent: number
  failed: number
  pending: number
  total: number
}

export interface TimelineStats {
  points: TimelinePoint[]
}

export interface DeliveryFunnel {
  created: number
  queued: number
  processing: number
  sent: number
  failed: number
  deadLettered: number
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

export interface ProviderStats {
  providers: ProviderBreakdown[]
}

export interface ProviderBreakdown {
  provider: string
  sent: number
  failed: number
  successRate: number
  avgLatencyMs: number
}

export interface UsageStats {
  currentMonth: number
  lastMonth: number
  dailyAverage: number
  peakDay: string
  peakCount: number
}