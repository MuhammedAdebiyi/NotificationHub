// ─── Overview ────────────────────────────────────────────────────────────────


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
  deadLettersNeedingReview: number      
  campaignsRunning: number
  campaignsScheduled: number
  apiCallsToday: number
  plan: string
  planUsagePct: number
  estimatedQueueDrainMinutes: number   
  oldestPendingSeconds: number           
}

// ─── Timeline ────────────────────────────────────────────────────────────────

export interface TimelinePoint {
  hour: string
  queued: number
  processing: number
  retrying: number
  failed: number
  sent: number
}

// ─── Delivery Funnel ─────────────────────────────────────────────────────────


export interface DeliveryFunnel {
  queued: number
  processing: number
  sent: number
  failed: number
  deadLetter: number
}

// ─── Queue ───────────────────────────────────────────────────────────────────


export interface QueueSnapshot {
  pending: number
  processing: number
  retrying: number
  deadLetter: number
  avgWaitMs: number
  workersActive: number
  workerCapacity: number
  maxConcurrency: number
  throughputPerMinute: number
  oldestPendingSeconds: number
  estimatedDrainMinutes: number
}

// ─── Campaigns ───────────────────────────────────────────────────────────────

export interface CampaignAnalytics {
  running: number
  scheduled: number
  drafts: number
  completedToday: number
  largestCampaign: number
  averageCampaignSize: number
  averageCompletionMinutes: number
  longestRunningMinutes: number
  recent: RecentCampaign[]
}

// Matches RecentCampaignDto
export interface RecentCampaign {
  id: string
  title: string
  recipientCount: number             
  status: 'Running' | 'Completed' | 'Scheduled' | 'Draft'
  progressPercent: number | null       
  scheduledAt: string | null
}

// ─── Failures ────────────────────────────────────────────────────────────────


export interface FailureDto {
  notificationId: string              
  title: string                       
  provider: string
  failureType: 'smtp_timeout' | 'rate_limited' | 'invalid_email' | 'dead_letter' | 'unknown'
  reason: string                       
  retryCount: number
  campaign: string | null
  suggestedAction: string
  occurredAt: string                   
}

// ─── Health ──────────────────────────────────────────────────────────────────


export interface AnalyticsHealth {
  overallStatus: 'healthy' | 'warning' | 'critical'
  successRate: number
  queueLatencyMs: number
  workersOnline: number
  incidentMessage: string | null
  components: ComponentHealth[]
}

export interface ComponentHealth {
  name: string
  status: 'healthy' | 'degraded' | 'down'
  latencyMs: number | null
  workers: number | null
  successRate: number | null
}

// ─── Infrastructure ──────────────────────────────────────────────────────────


export interface InfrastructureHealth {
  database: { status: string; latencyMs: number }
  redis:    { status: string; latencyMs: number }
  workers:  { status: string; online: number; capacity: number }
  provider: { name: string; status: string; successRate: number; avgLatencyMs: number }
  api:      { status: string; uptime: string }
}