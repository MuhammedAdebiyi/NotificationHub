import { useState, useEffect } from 'react'
import AppLayout from '@/app/layouts/AppLayout'
import { apiClient } from '@/shared/api/apiClient'

interface Stats {
  totalSent: number
  totalFailed: number
  totalPending: number
  successRate: string
  avgDeliveryTime: string
  todaySent: number
  weekSent: number
  monthSent: number
}

interface ProviderStat {
  provider: string
  count: number
  successRate: string
}

interface DailyStat {
  date: string
  sent: number
  failed: number
}

export default function DeveloperAnalyticsPage() {
  const [stats, setStats] = useState<Stats | null>(null)
  const [providerStats, setProviderStats] = useState<ProviderStat[]>([])
  const [dailyStats, setDailyStats] = useState<DailyStat[]>([])
  const [loading, setLoading] = useState(true)

  useEffect(() => { load() }, [])

  async function load() {
    try {
      const [s, p, d] = await Promise.all([
        apiClient.get<Stats>('/api/v1/analytics/overview'),
        apiClient.get<{ items: ProviderStat[] }>('/api/v1/analytics/providers'),
        apiClient.get<{ items: DailyStat[] }>('/api/v1/analytics/daily?days=30'),
      ])
      setStats(s)
      setProviderStats(p.items || [])
      setDailyStats(d.items || [])
    } catch { /* ignore */ }
    setLoading(false)
  }

  const statCards = stats ? [
    { label: 'Total Sent', value: stats.totalSent.toLocaleString(), color: 'text-violet' },
    { label: 'Success Rate', value: stats.successRate, color: 'text-teal' },
    { label: 'Today', value: stats.todaySent.toLocaleString(), color: 'text-violet' },
    { label: 'This Week', value: stats.weekSent.toLocaleString(), color: 'text-violet' },
    { label: 'Failed', value: stats.totalFailed.toLocaleString(), color: 'text-coral' },
    { label: 'Pending', value: stats.totalPending.toLocaleString(), color: 'text-yellow' },
  ] : []

  return (
    <AppLayout>
      <div className="mb-8">
        <p className="hand text-xl text-violet mb-1">developer dashboard —</p>
        <h1 className="font-display font-bold text-3xl">Analytics</h1>
      </div>

      {loading ? (
        <div className="text-center py-12 text-ink/40 text-sm">Loading analytics...</div>
      ) : !stats ? (
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
              </div>
            ))}
          </div>

          <div className="grid lg:grid-cols-2 gap-6">
            <div className="bg-white border border-ink/10 rounded-xl p-6">
              <h3 className="font-display font-bold text-lg mb-4">By Provider</h3>
              {providerStats.length === 0 ? (
                <p className="text-sm text-ink/40">No provider data</p>
              ) : (
                <div className="space-y-3">
                  {providerStats.map(p => (
                    <div key={p.provider} className="flex items-center justify-between">
                      <div className="flex items-center gap-2">
                        <span className="font-medium text-sm">{p.provider}</span>
                        <span className="text-xs text-ink/40">{p.count} sent</span>
                      </div>
                      <span className={`text-sm font-medium ${parseFloat(p.successRate) >= 95 ? 'text-teal' : parseFloat(p.successRate) >= 80 ? 'text-yellow' : 'text-coral'}`}>
                        {p.successRate}
                      </span>
                    </div>
                  ))}
                </div>
              )}
            </div>

            <div className="bg-white border border-ink/10 rounded-xl p-6">
              <h3 className="font-display font-bold text-lg mb-4">Last 30 Days</h3>
              {dailyStats.length === 0 ? (
                <p className="text-sm text-ink/40">No daily data</p>
              ) : (
                <div className="space-y-1">
                  {dailyStats.slice(-10).map(d => (
                    <div key={d.date} className="flex items-center gap-3 text-xs">
                      <span className="text-ink/40 w-20">{d.date.slice(5)}</span>
                      <div className="flex-1 h-4 bg-fog rounded-full overflow-hidden flex">
                        <div
                          className="h-full bg-teal/60"
                          style={{ width: `${d.sent + d.failed > 0 ? (d.sent / (d.sent + d.failed)) * 100 : 0}%` }}
                        />
                        <div
                          className="h-full bg-coral/60"
                          style={{ width: `${d.sent + d.failed > 0 ? (d.failed / (d.sent + d.failed)) * 100 : 0}%` }}
                        />
                      </div>
                      <span className="text-ink/60 w-16 text-right">{d.sent} sent</span>
                    </div>
                  ))}
                </div>
              )}
            </div>
          </div>
        </>
      )}
    </AppLayout>
  )
}
