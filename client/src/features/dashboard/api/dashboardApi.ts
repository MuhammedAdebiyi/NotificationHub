import { apiClient } from '@/shared/services/apiClient'
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

interface RawHealth {
  overallStatus: string
  successRate: number
  queueLatencyMs: number
  workersOnline: number
  incidentMessage?: string
}

interface RawTimelinePoint {
  hour?: string
  sent?: number | string
  failed?: number | string
  retrying?: number | string
}

interface RawQueue {
  pending?: number
  queueDepth?: number
  processing?: number
  retrying?: number
  deadLetter?: number
  dlqLength?: number
  avgWaitMs?: number
  workersActive?: number
  workers?: number
  workerCapacity?: number
  throughputPerMinute?: number
  throughputPerMin?: number
}

interface RawCampaignRecent {
  id?: string
  title?: string
  totalRecipients?: number
  recipientCount?: number
  status?: string
  progressPercent?: number
  deliveryRate?: number
  scheduledAt?: string
}

interface RawCampaignSnapshot {
  running?: number
  scheduled?: number
  drafts?: number
  draft?: number
  completedToday?: number
  recent?: RawCampaignRecent[]
  topCampaigns?: RawCampaignRecent[]
}

interface RawFailure {
  notificationId?: string
  publicId?: string
  title?: string
  type?: string
  reason?: string
  errorMessage?: string
  failureType?: string
  status?: string
  occurredAt?: string
  createdAt?: string
  provider?: string
  retryCount?: number
  suggestedAction?: string
}

interface RawActivity {
  id?: string
  timestamp?: string
  time?: string
  type?: string
  label?: string
  title?: string
  subtitle?: string
  channel?: string
}

interface RawInfra {
  database?: { status?: string; latencyMs?: number }
  redis?: { status?: string; latencyMs?: number }
  workers?: { status?: string; online?: number; capacity?: number }
  provider?: { name?: string; status?: string; successRate?: number }
  api?: { status?: string; uptime?: string }
}

export const dashboardApi = {
  getHealth: () =>
    apiClient.get<RawHealth>('/api/v1/analytics/health').then(r => ({
      status: r.overallStatus,
      successRate: r.successRate,
      queueLatencyMs: r.queueLatencyMs,
      workersOnline: r.workersOnline,
      incidentMessage: r.incidentMessage ?? undefined,
    } as HealthBannerData)),

  getTodayStats: () =>
    apiClient.get<TodayStats>('/api/v1/analytics/overview'),

  getDeliveryTimeline: () =>
    apiClient.get<RawTimelinePoint[]>('/api/v1/analytics/timeline').then(r =>
      (Array.isArray(r) ? r : []).map(p => ({
        hour:     p.hour     ?? '',
        sent:     Number(p.sent     ?? 0),
        failed:   Number(p.failed   ?? 0),
        retrying: Number(p.retrying ?? 0),
      })) as DeliveryPoint[]
    ),

  getQueue: () =>
    apiClient.get<RawQueue>('/api/v1/analytics/queue').then(r => ({
      pending:          r.pending          ?? r.queueDepth    ?? 0,
      processing:       r.processing       ?? 0,
      retrying:         r.retrying         ?? 0,
      deadLetter:       r.deadLetter       ?? r.dlqLength     ?? 0,
      avgWaitMs:        r.avgWaitMs        ?? 0,
      workersActive:    r.workersActive    ?? r.workers       ?? 0,
      workerCapacity:   r.workerCapacity   ?? 4,
      throughputPerMin: r.throughputPerMinute ?? r.throughputPerMin ?? 0,
    } as QueueStats)),

  getCampaignSnapshot: () =>
    apiClient.get<RawCampaignSnapshot>('/api/v1/analytics/campaigns').then(r => ({
      running:        r.running        ?? 0,
      scheduled:      r.scheduled      ?? 0,
      drafts:         r.drafts         ?? r.draft ?? 0,
      completedToday: r.completedToday ?? 0,
      recent: (r.recent ?? r.topCampaigns ?? []).map(c => ({
        id:              c.id ?? '',
        title:           c.title ?? '',
        recipientCount:  c.totalRecipients ?? c.recipientCount ?? 0,
        status:          (c.status ?? 'Draft') as CampaignSnapshot['recent'][number]['status'],
        progressPercent: c.progressPercent ?? c.deliveryRate ?? undefined,
        scheduledAt:     c.scheduledAt ?? undefined,
      })),
    } as CampaignSnapshot)),

  getRecentFailures: () =>
    apiClient.get<RawFailure[]>('/api/v1/analytics/failures').then(r =>
      (Array.isArray(r) ? r : []).map(f => ({
        notificationId: f.notificationId ?? f.publicId ?? '',
        title:          f.title          ?? f.type     ?? 'Unknown',
        reason:         f.reason         ?? f.errorMessage ?? '',
        failureType:    (f.failureType ?? 'unknown') as RecentFailure['failureType'],
        status:         (f.status ?? 'failed') as RecentFailure['status'],
        occurredAt:     f.occurredAt     ?? f.createdAt ?? '',
        provider:       f.provider       ?? '',
        retryCount:     f.retryCount     ?? 0,
        suggestedAction: f.suggestedAction ?? '',
      })) as RecentFailure[]
    ),

  getActivity: () =>
    apiClient.get<RawActivity[]>('/api/v1/analytics/activity').then(r =>
      (Array.isArray(r) ? r : []).map(a => ({
        id:       a.id       ?? '',
        time:     a.timestamp
                    ? new Date(a.timestamp).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })
                    : a.time ?? '',
        type:     (a.type ?? 'sent') as ActivityItem['type'],
        title:    a.title    ?? a.label    ?? '',
        subtitle: a.subtitle ?? a.channel  ?? '',
      })) as ActivityItem[]
    ),

  getInfrastructure: () =>
    apiClient.get<RawInfra>('/api/v1/analytics/infrastructure').then(r => ({
      services: [
        {
          name:   'Database',
          status: (r.database?.status ?? 'down') as InfrastructureHealth['services'][number]['status'],
          detail: r.database?.latencyMs != null ? `${r.database.latencyMs}ms` : '—',
        },
        {
          name:   'Redis',
          status: (r.redis?.status ?? 'down') as InfrastructureHealth['services'][number]['status'],
          detail: r.redis?.latencyMs != null ? `${r.redis.latencyMs}ms` : '—',
        },
        {
          name:   'Worker',
          status: (r.workers?.status ?? 'down') as InfrastructureHealth['services'][number]['status'],
          detail: r.workers?.online != null
            ? `${r.workers.online} / ${r.workers.capacity} slots`
            : '—',
        },
        {
          name:   r.provider?.name ?? 'Provider',
          status: (r.provider?.status ?? 'down') as InfrastructureHealth['services'][number]['status'],
          detail: r.provider?.successRate != null
            ? `${r.provider.successRate}% success`
            : '—',
        },
        {
          name:   'API',
          status: (r.api?.status ?? 'down') as InfrastructureHealth['services'][number]['status'],
          detail: r.api?.uptime ?? '—',
        },
      ],
    } as InfrastructureHealth)),

  getOrgInfo: () =>
    apiClient.get<{ name: string; plan: string }>('/api/v1/org/info'),
}
