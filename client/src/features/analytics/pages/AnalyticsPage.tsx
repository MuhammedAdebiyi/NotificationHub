import { useState, useEffect } from 'react'
import AppLayout from '@/app/layouts/AppLayout'
import { apiClient } from '@/shared/services/apiClient'

interface DashboardStats {
  totalSent: number
  pending: number
  failed: number
  successRate: number
  queueLength: number
  activeUsers: number
}

interface ActivityItem {
  id: string
  label: string
  channel: string
  status: string
  timestamp: string
}

export default function AnalyticsPage() {
  const [stats, setStats] = useState<DashboardStats | null>(null)
  const [activity, setActivity] = useState<ActivityItem[]>([])
  const [isLoading, setIsLoading] = useState(true)

  useEffect(() => {
    Promise.all([
      apiClient.get<DashboardStats>('/api/v1/dashboard/stats'),
      apiClient.get<ActivityItem[]>('/api/v1/dashboard/activity'),
    ])
      .then(([s, a]) => { setStats(s); setActivity(a) })
      .catch(() => {})
      .finally(() => setIsLoading(false))
  }, [])

  const statusCounts = activity.reduce<Record<string, number>>((acc, item) => {
    acc[item.status] = (acc[item.status] ?? 0) + 1
    return acc
  }, {})

  const channelCounts = activity.reduce<Record<string, number>>((acc, item) => {
    acc[item.channel] = (acc[item.channel] ?? 0) + 1
    return acc
  }, {})

  const barColor: Record<string, string> = {
    Sent: 'bg-teal',
    Pending: 'bg-yellow-400',
    Failed: 'bg-coral',
    DeadLetter: 'bg-coral/60',
    Retrying: 'bg-violet',
    Processing: 'bg-ink/30',
  }

  return (
    <AppLayout>
      <div className="mb-8">
        <p className="hand text-xl text-violet mb-1">delivery insights —</p>
        <h1 className="font-display font-bold text-3xl">Analytics</h1>
      </div>

      {isLoading ? (
        <p className="text-sm text-ink/40">Loading...</p>
      ) : (
        <div className="space-y-6">
          {/* Summary row */}
          <div className="grid grid-cols-2 sm:grid-cols-4 gap-4">
            {[
              { label: 'Total Sent', value: stats?.totalSent ?? 0, color: 'text-teal' },
              { label: 'Pending', value: stats?.pending ?? 0, color: 'text-yellow-500' },
              { label: 'Failed', value: stats?.failed ?? 0, color: 'text-coral' },
              { label: 'Success Rate', value: `${stats?.successRate ?? 0}%`, color: 'text-violet' },
            ].map(card => (
              <div key={card.label} className="bg-fog border border-ink/10 rounded-xl p-5">
                <p className={`font-display font-extrabold text-3xl ${card.color}`}>{card.value}</p>
                <p className="text-xs text-ink/50 mt-1">{card.label}</p>
              </div>
            ))}
          </div>

          {/* Status breakdown */}
          <div className="bg-fog border border-ink/10 rounded-xl p-6">
            <h2 className="font-display font-bold text-lg mb-5">Status Breakdown</h2>
            {Object.keys(statusCounts).length === 0 ? (
              <p className="text-sm text-ink/40 text-center py-6">No data yet.</p>
            ) : (
              <div className="space-y-3">
                {Object.entries(statusCounts).map(([status, count]) => {
                  const total = Object.values(statusCounts).reduce((a, b) => a + b, 0)
                  const pct = Math.round((count / total) * 100)
                  return (
                    <div key={status}>
                      <div className="flex justify-between text-sm mb-1">
                        <span className="font-medium">{status}</span>
                        <span className="text-ink/50">{count} ({pct}%)</span>
                      </div>
                      <div className="h-2 bg-ink/10 rounded-full overflow-hidden">
                        <div
                          className={`h-full rounded-full ${barColor[status] ?? 'bg-ink/30'}`}
                          style={{ width: `${pct}%` }}
                        />
                      </div>
                    </div>
                  )
                })}
              </div>
            )}
          </div>

          {/* Channel breakdown */}
          <div className="bg-fog border border-ink/10 rounded-xl p-6">
            <h2 className="font-display font-bold text-lg mb-5">Channel Breakdown</h2>
            {Object.keys(channelCounts).length === 0 ? (
              <p className="text-sm text-ink/40 text-center py-6">No data yet.</p>
            ) : (
              <div className="flex gap-4 flex-wrap">
                {Object.entries(channelCounts).map(([channel, count]) => (
                  <div key={channel} className="bg-white border border-ink/10 rounded-xl px-6 py-4 text-center">
                    <p className="font-display font-extrabold text-2xl text-violet">{count}</p>
                    <p className="text-xs text-ink/50 mt-1">{channel}</p>
                  </div>
                ))}
              </div>
            )}
          </div>

          {/* Recent activity */}
          <div className="bg-fog border border-ink/10 rounded-xl p-6">
            <h2 className="font-display font-bold text-lg mb-4">Recent Activity</h2>
            {activity.length === 0 ? (
              <p className="text-sm text-ink/40 text-center py-6">No activity yet.</p>
            ) : (
              <div className="divide-y divide-ink/5">
                {activity.map(item => (
                  <div key={item.id} className="flex items-center justify-between py-3">
                    <div>
                      <span className="text-sm font-medium">{item.label}</span>
                      <span className="text-xs text-ink/40 ml-2">{item.channel}</span>
                    </div>
                    <div className="flex items-center gap-3">
                      <span className="text-xs text-ink/30">
                        {new Date(item.timestamp).toLocaleString()}
                      </span>
                      <span className={`text-xs font-medium ${
                        item.status === 'Sent' ? 'text-teal' :
                        item.status === 'Failed' ? 'text-coral' : 'text-ink/40'
                      }`}>
                        {item.status}
                      </span>
                    </div>
                  </div>
                ))}
              </div>
            )}
          </div>
        </div>
      )}
    </AppLayout>
  )
}