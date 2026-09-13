import { useState, useEffect } from 'react'
import AppLayout from '@/app/layouts/AppLayout'
import { apiClient } from '@/shared/services/apiClient'

interface Overview {
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
  apiCallsToday: number
}

interface ProviderStat {
  provider: string
  successRate: number
  avgLatencyMs: number
  sentToday: number
}

interface TimelinePoint {
  hour: string
  sent: number
  failed: number
  retrying: number
}

export default function DeveloperAnalyticsPage() {
  const [overview, setOverview] = useState<Overview | null>(null)
  const [providers, setProviders] = useState<ProviderStat[]>([])
  const [timeline, setTimeline] = useState<TimelinePoint[]>([])
  const [loading, setLoading] = useState(true)

  useEffect(() => { load() }, [])

  async function load() {
    try {
      const [o, p, t] = await Promise.all([
        apiClient.get<Overview>('/api/v1/analytics/overview'),
        apiClient.get<ProviderStat[]>('/api/v1/analytics/providers'),
        apiClient.get<TimelinePoint[]>('/api/v1/analytics/timeline'),
      ])
      setOverview(o)
      setProviders(Array.isArray(p) ? p : [])
      setTimeline(Array.isArray(t) ? t : [])
    } catch { /* ignore */ }
    setLoading(false)
  }

  const statCards = overview ? [
    { label: 'Sent Today', value: overview.notificationsSent.toLocaleString(), color: 'text-violet', delta: overview.notificationsSentDelta },
    { label: 'Success Rate', value: `${overview.successRate}%`, color: 'text-teal', delta: overview.successRateDelta },
    { label: 'Avg Send Time', value: `${overview.avgSendTimeMs}ms`, color: 'text-violet' },
    { label: 'P95 Send Time', value: `${overview.p95SendTimeMs}ms`, color: 'text-violet' },
    { label: 'Dead Letters', value: overview.deadLetters.toLocaleString(), color: 'text-coral' },
    { label: 'Queue Depth', value: overview.queueDepth.toLocaleString(), color: 'text-yellow' },
  ] : []

  return (
    <AppLayout>
      <div className="mb-8">
        <p className="hand text-xl text-violet mb-1">developer dashboard —</p>
        <h1 className="font-display font-bold text-3xl">Analytics</h1>
      </div>

      {loading ? (
        <div className="text-center py-12 text-ink/40 text-sm">Loading analytics...</div>
      ) : !overview ? (
        <div className="bg-white border border-ink/10 rounded-xl p-12 text-center">
          <p className="font-display font-bold text-lg mb-2">No data yet</p>
          <p className="text-sm text-ink/50">Send some notifications to see analytics.</p>
        </div>
      ) : (
        <>
          <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-6 gap-4 mb-8">
            {statCards.map(s => (
              <div key={s.label} className="bg-white border border-ink/10 rounded-xl p-4 text-center">
                <p className={`font-display font-extrabold text-2xl ${s.color}`}>{s.value}</p>
                <p className="text-xs text-ink/50 mt-1">{s.label}</p>
                {s.delta !== undefined && (
                  <p className={`text-xs mt-1 ${s.delta >= 0 ? 'text-teal' : 'text-coral'}`}>
                    {s.delta >= 0 ? '+' : ''}{s.delta.toFixed(1)}% vs yesterday
                  </p>
                )}
              </div>
            ))}
          </div>

          <div className="grid lg:grid-cols-2 gap-6">
            <div className="bg-white border border-ink/10 rounded-xl p-6">
              <h3 className="font-display font-bold text-lg mb-4">By Provider</h3>
              {providers.length === 0 ? (
                <p className="text-sm text-ink/40">No provider data</p>
              ) : (
                <div className="space-y-3">
                  {providers.map(p => (
                    <div key={p.provider} className="flex items-center justify-between">
                      <div className="flex items-center gap-2">
                        <span className="font-medium text-sm">{p.provider}</span>
                        <span className="text-xs text-ink/40">{p.sentToday} sent today</span>
                      </div>
                      <div className="flex items-center gap-3">
                        <span className="text-xs text-ink/40">{p.avgLatencyMs}ms avg</span>
                        <span className={`text-sm font-medium ${p.successRate >= 95 ? 'text-teal' : p.successRate >= 80 ? 'text-yellow' : 'text-coral'}`}>
                          {p.successRate}%
                        </span>
                      </div>
                    </div>
                  ))}
                </div>
              )}
            </div>

            <div className="bg-white border border-ink/10 rounded-xl p-6">
              <h3 className="font-display font-bold text-lg mb-4">Today&apos;s Timeline</h3>
              {timeline.length === 0 ? (
                <p className="text-sm text-ink/40">No timeline data</p>
              ) : (
                <div className="space-y-1">
                  {timeline.map(t => {
                    const maxVal = Math.max(t.sent + t.failed + t.retrying, 1)
                    return (
                      <div key={t.hour} className="flex items-center gap-3 text-xs">
                        <span className="text-ink/40 w-12">{t.hour}</span>
                        <div className="flex-1 h-4 bg-fog rounded-full overflow-hidden flex">
                          <div className="h-full bg-teal/60" style={{ width: `${(t.sent / maxVal) * 100}%` }} />
                          <div className="h-full bg-yellow/60" style={{ width: `${(t.retrying / maxVal) * 100}%` }} />
                          <div className="h-full bg-coral/60" style={{ width: `${(t.failed / maxVal) * 100}%` }} />
                        </div>
                        <span className="text-ink/60 w-16 text-right">{t.sent} sent</span>
                      </div>
                    )
                  })}
                </div>
              )}
            </div>
          </div>

          <div className="mt-6 bg-white border border-ink/10 rounded-xl p-6">
            <h3 className="font-display font-bold text-lg mb-4">Infrastructure</h3>
            <div className="grid sm:grid-cols-3 gap-4 text-sm">
              <div>
                <span className="text-ink/50">Workers Online</span>
                <p className="font-display font-bold text-xl text-violet">{overview.workersActive}</p>
              </div>
              <div>
                <span className="text-ink/50">API Calls Today</span>
                <p className="font-display font-bold text-xl text-violet">{overview.apiCallsToday.toLocaleString()}</p>
              </div>
              <div>
                <span className="text-ink/50">Dead Letters Need Review</span>
                <p className={`font-display font-bold text-xl ${overview.deadLettersNeedingReview > 0 ? 'text-coral' : 'text-teal'}`}>
                  {overview.deadLettersNeedingReview}
                </p>
              </div>
            </div>
          </div>
        </>
      )}
    </AppLayout>
  )
}
