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

export const dashboardApi = {
  getHealth: () =>
    apiClient.get<any>('/api/v1/analytics/health').then(r => ({
      status: r.overallStatus,
      successRate: r.successRate,
      queueLatencyMs: r.queueLatencyMs,
      workersOnline: r.workersOnline,
      incidentMessage: r.incidentMessage ?? undefined,
    } as HealthBannerData)),

  getTodayStats: () =>
    apiClient.get<TodayStats>('/api/v1/analytics/overview'),

  getDeliveryTimeline: () =>
    // Backend returns array directly — no wrapper object
    apiClient.get<any[]>('/api/v1/analytics/timeline').then(r =>
      (Array.isArray(r) ? r : []).map((p: any) => ({
        hour:     p.hour     ?? '',
        sent:     Number(p.sent     ?? 0),   // force number — chart breaks on strings
        failed:   Number(p.failed   ?? 0),
        retrying: Number(p.retrying ?? 0),
      })) as DeliveryPoint[]
    ),

  getQueue: () =>
    apiClient.get<any>('/api/v1/analytics/queue').then(r => ({
      pending:          r.pending          ?? r.queueDepth    ?? 0,
      processing:       r.processing       ?? 0,
      retrying:         r.retrying         ?? 0,
      deadLetter:       r.deadLetter       ?? r.dlqLength     ?? 0,
      avgWaitMs:        r.avgWaitMs        ?? 0,
      workersActive:    r.workersActive    ?? r.workers       ?? 0,
      workerCapacity:   r.workerCapacity   ?? 4,
      throughputPerMin: r.throughputPerMinute ?? r.throughputPerMin ?? 0,  // backend key is throughputPerMinute
    } as QueueStats)),

  getCampaignSnapshot: () =>
    apiClient.get<any>('/api/v1/analytics/campaigns').then(r => ({
      running:        r.running        ?? 0,
      scheduled:      r.scheduled      ?? 0,
      drafts:         r.drafts         ?? r.draft ?? 0,
      completedToday: r.completedToday ?? 0,   // ← was r.completed — wrong field name
      recent: (r.recent ?? r.topCampaigns ?? []).map((c: any) => ({
        id:              c.id,
        title:           c.title,
        recipientCount:  c.totalRecipients ?? c.recipientCount ?? 0,
        status:          c.status,
        progressPercent: c.progressPercent ?? c.deliveryRate ?? undefined,  // backend sends progressPercent
        scheduledAt:     c.scheduledAt ?? undefined,
      })),
    } as CampaignSnapshot)),

  getRecentFailures: () =>
    apiClient.get<any[]>('/api/v1/analytics/failures').then(r =>
      (Array.isArray(r) ? r : []).map((f: any) => ({
        notificationId: f.notificationId ?? f.publicId ?? '',
        title:          f.title          ?? f.type     ?? 'Unknown',      // backend sends "title"
        reason:         f.reason         ?? f.errorMessage ?? '',          // backend sends "reason"
        failureType:    f.failureType    ?? 'unknown',
        status:         f.status         ?? 'failed',
        occurredAt:     f.occurredAt     ?? f.createdAt ?? '',             // backend sends "occurredAt"
        provider:       f.provider       ?? '',
        retryCount:     f.retryCount     ?? 0,
        suggestedAction: f.suggestedAction ?? '',
      })) as RecentFailure[]
    ),

  getActivity: () =>
    apiClient.get<any[]>('/api/v1/analytics/activity').then(r =>
      (Array.isArray(r) ? r : []).map((a: any) => ({
        id:       a.id       ?? '',
        time:     a.timestamp
                    ? new Date(a.timestamp).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })
                    : a.time ?? '',                                        // format timestamp → "09:32"
        type:     a.type     ?? 'sent',
        title:    a.title    ?? a.label    ?? '',
        subtitle: a.subtitle ?? a.channel  ?? '',
      })) as ActivityItem[]
    ),

  getInfrastructure: () =>
    apiClient.get<any>('/api/v1/analytics/infrastructure').then(r => ({
      services: [
        {
          name:   'Database',
          status: r.database?.status ?? 'down',
          detail: r.database?.latencyMs != null ? `${r.database.latencyMs}ms` : '—',
        },
        {
          name:   'Redis',
          status: r.redis?.status ?? 'down',
          detail: r.redis?.latencyMs != null ? `${r.redis.latencyMs}ms` : '—',
        },
        {
          name:   'Worker',
          status: r.workers?.status ?? 'down',
          detail: r.workers?.online != null
            ? `${r.workers.online} / ${r.workers.capacity} slots`
            : '—',
        },
        {
          name:   r.provider?.name ?? 'Provider',
          status: r.provider?.status ?? 'down',
          detail: r.provider?.successRate != null
            ? `${r.provider.successRate}% success`
            : '—',
        },
        {
          name:   'API',
          status: r.api?.status ?? 'down',
          detail: r.api?.uptime ?? '—',
        },
      ],
    } as InfrastructureHealth)),

  getOrgInfo: () =>
    apiClient.get<{ name: string; plan: string }>('/api/v1/org/info'),
}