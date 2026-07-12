import AppLayout from '@/app/layouts/AppLayout'
import { useAnalytics } from '../hooks/useAnalytics'
import type { RecentCampaign, FailureDto } from '../types/analytics.types'
import {
  AreaChart, Area, XAxis, YAxis, CartesianGrid, Tooltip,
  ResponsiveContainer, BarChart, Bar, Cell,
} from 'recharts'

function KpiCard({
  label,
  value,
  sub,
  accent = 'violet',
}: {
  label: string
  value: string | number
  sub?: string
  accent?: 'violet' | 'teal' | 'coral' | 'yellow'
}) {
  const colors = {
    violet: 'text-violet',
    teal:   'text-teal',
    coral:  'text-coral',
    yellow: 'text-ink',
  }
  return (
    <div className="bg-fog border border-ink/10 rounded-xl p-5">
      <p className={`font-display font-extrabold text-3xl ${colors[accent]}`}>{value}</p>
      <p className="text-xs text-ink/50 mt-1">{label}</p>
      {sub && <p className="text-xs text-ink/40 mt-0.5">{sub}</p>}
    </div>
  )
}

function Section({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <div className="bg-fog border border-ink/10 rounded-xl p-6">
      <h2 className="font-display font-bold text-lg mb-4">{title}</h2>
      {children}
    </div>
  )
}

export default function AnalyticsPage() {
  const { overview, timeline, funnel, queue, campaigns, failures, isLoading } = useAnalytics()

  if (isLoading) {
    return (
      <AppLayout>
        <div className="flex items-center justify-center h-64 text-ink/40 text-sm">
          Loading analytics...
        </div>
      </AppLayout>
    )
  }

  const funnelTotal = funnel
    ? funnel.queued + funnel.processing + funnel.sent + funnel.failed + funnel.deadLetter
    : 0

  return (
    <AppLayout>
      <div className="mb-8">
        <p className="hand text-xl text-violet mb-1">how are we doing —</p>
        <h1 className="font-display font-bold text-3xl">Analytics</h1>
      </div>

      {/* Overview KPIs */}
      <div className="grid grid-cols-2 sm:grid-cols-4 gap-4 mb-6">
        <KpiCard
          label="Total Sent"
          value={overview?.notificationsSent.toLocaleString() ?? '—'}
          accent="teal"
        />
        <KpiCard
          label="Success Rate"
          value={overview ? `${overview.successRate}%` : '—'}
          sub={
            overview?.successRateDelta !== 0
              ? `${overview!.successRateDelta > 0 ? '+' : ''}${overview!.successRateDelta}% vs yesterday`
              : undefined
          }
          accent="violet"
        />
        <KpiCard
          label="Dead Letters"
          value={overview?.deadLetters.toLocaleString() ?? '—'}
          sub={
            overview?.deadLettersNeedingReview
              ? `${overview.deadLettersNeedingReview} need review`
              : undefined
          }
          accent="coral"
        />
        <KpiCard
          label="Avg Send Time"
          value={overview ? `${overview.avgSendTimeMs}ms` : '—'}
          sub={overview ? `p95: ${overview.p95SendTimeMs}ms` : undefined}
          accent="yellow"
        />
      </div>

      {/* Queue KPIs — use backend field names directly */}
      {queue && (
        <div className="grid grid-cols-2 sm:grid-cols-4 gap-4 mb-6">
          <KpiCard label="Queue right now" value={queue.pending} accent="violet" />
          <KpiCard label="Dead Letter Queue" value={queue.deadLetter}                        accent="coral" />
          <KpiCard label="Throughput"        value={`${queue.throughputPerMinute}/min`}      accent="teal" />
          <KpiCard label="Avg Wait"          value={`${queue.avgWaitMs}ms`}                  accent="yellow"
            sub={`Drain in ~${queue.estimatedDrainMinutes}min`}
          />
        </div>
      )}

      <div className="grid lg:grid-cols-3 gap-6 mb-6">
        {/* Volume Timeline */}
        <div className="lg:col-span-2">
          <Section title="Notification Volume — Last 24h">
            {timeline.length > 0 && timeline.some(p => p.sent > 0 || p.failed > 0) ? (
              <ResponsiveContainer width="100%" height={220}>
                <AreaChart data={timeline}>
                  <defs>
                    <linearGradient id="sentGrad" x1="0" y1="0" x2="0" y2="1">
                      <stop offset="5%"  stopColor="#0E9F84" stopOpacity={0.3} />
                      <stop offset="95%" stopColor="#0E9F84" stopOpacity={0} />
                    </linearGradient>
                    <linearGradient id="failedGrad" x1="0" y1="0" x2="0" y2="1">
                      <stop offset="5%"  stopColor="#FF6452" stopOpacity={0.3} />
                      <stop offset="95%" stopColor="#FF6452" stopOpacity={0} />
                    </linearGradient>
                  </defs>
                  <CartesianGrid strokeDasharray="3 3" stroke="#e5e7eb" />
                  <XAxis
                    dataKey="hour"
                    tick={{ fontSize: 11, fill: '#9ca3af' }}
                    tickLine={false}
                    interval={3}
                  />
                  <YAxis
                    tick={{ fontSize: 11, fill: '#9ca3af' }}
                    tickLine={false}
                    axisLine={false}
                  />
                  <Tooltip
                    contentStyle={{
                      background: '#fff',
                      border: '1px solid #e5e7eb',
                      borderRadius: 8,
                      fontSize: 12,
                    }}
                  />
                  <Area type="monotone" dataKey="sent"     stroke="#0E9F84" strokeWidth={2}   fill="url(#sentGrad)"   name="Sent" />
                  <Area type="monotone" dataKey="failed"   stroke="#FF6452" strokeWidth={2}   fill="url(#failedGrad)" name="Failed" />
                  <Area type="monotone" dataKey="retrying" stroke="#FFC83D" strokeWidth={1.5} fill="none"             name="Retrying" strokeDasharray="4 2" />
                </AreaChart>
              </ResponsiveContainer>
            ) : (
              <div className="h-48 flex items-center justify-center text-ink/30 text-sm border border-dashed border-ink/15 rounded-lg">
                No notification activity in the last 24 hours
              </div>
            )}
          </Section>
        </div>

        {/* Delivery Funnel */}
        <Section title="Delivery Funnel">
          {funnel && funnelTotal > 0 ? (
            <div className="space-y-3">
              {[
                { label: 'Queued',      value: funnel.queued,      color: 'bg-violet/70' },
                { label: 'Processing',  value: funnel.processing,  color: 'bg-yellow/60' },
                { label: 'Sent',        value: funnel.sent,        color: 'bg-teal' },
                { label: 'Failed',      value: funnel.failed,      color: 'bg-coral/70' },
                { label: 'Dead Letter', value: funnel.deadLetter,  color: 'bg-coral' },
              ].map(row => (
                <div key={row.label} className="flex items-center gap-3">
                  <div className="w-24 text-xs text-ink/50 text-right shrink-0">{row.label}</div>
                  <div className="flex-1 bg-ink/5 rounded-full h-2">
                    <div
                      className={`${row.color} h-2 rounded-full transition-all`}
                      style={{
                        width: funnelTotal > 0
                          ? `${Math.max(2, Math.round(row.value / funnelTotal * 100))}%`
                          : '0%',
                      }}
                    />
                  </div>
                  <div className="w-10 text-xs text-ink/60 font-medium text-right">
                    {row.value}
                  </div>
                </div>
              ))}
              <div className="pt-2 border-t border-ink/10 flex justify-between text-xs text-ink/40">
                <span>Total processed</span>
                <span className="font-medium">{funnelTotal.toLocaleString()}</span>
              </div>
            </div>
          ) : (
            <div className="text-center py-8 text-ink/30 text-sm">No delivery data yet</div>
          )}
        </Section>
      </div>

      <div className="grid lg:grid-cols-2 gap-6 mb-6">
        {/* Campaign Stats */}
        <Section title="Campaigns">
          {campaigns && (campaigns.running + campaigns.scheduled + campaigns.drafts + campaigns.completedToday) > 0 ? (
            <>
              <div className="grid grid-cols-2 gap-3 mb-4">
                <div className="bg-white rounded-lg p-3 border border-ink/10">
                  <p className="font-display font-bold text-2xl text-teal">{campaigns.completedToday}</p>
                  <p className="text-xs text-ink/50">Completed today</p>
                </div>
                <div className="bg-white rounded-lg p-3 border border-ink/10">
                  <p className="font-display font-bold text-2xl text-violet">{campaigns.running}</p>
                  <p className="text-xs text-ink/50">Running</p>
                </div>
                <div className="bg-white rounded-lg p-3 border border-ink/10">
                  <p className="font-display font-bold text-2xl text-ink">{campaigns.scheduled}</p>
                  <p className="text-xs text-ink/50">Scheduled</p>
                </div>
                <div className="bg-white rounded-lg p-3 border border-ink/10">
                  <p className="font-display font-bold text-2xl text-ink/50">{campaigns.drafts}</p>
                  <p className="text-xs text-ink/50">Drafts</p>
                </div>
              </div>

              {/* Recent campaigns bar chart — uses progressPercent as the bar value */}
              {campaigns.recent.length > 0 && (
                <ResponsiveContainer width="100%" height={160}>
                  <BarChart data={campaigns.recent.slice(0, 5)}>
                    <CartesianGrid strokeDasharray="3 3" stroke="#e5e7eb" />
                    <XAxis
                      dataKey="title"
                      tick={{ fontSize: 10, fill: '#9ca3af' }}
                      tickLine={false}
                    />
                    <YAxis
                      tick={{ fontSize: 10, fill: '#9ca3af' }}
                      tickLine={false}
                      axisLine={false}
                      domain={[0, 100]}
                      tickFormatter={v => `${v}%`}
                    />
                    <Tooltip
                      contentStyle={{ fontSize: 12, borderRadius: 8, border: '1px solid #e5e7eb' }}
                      formatter={(v) => [`${v ?? 0}%`, 'Progress']}
                    />
                    <Bar dataKey="progressPercent" name="Progress" radius={[4, 4, 0, 0]}>
                      {campaigns.recent.slice(0, 5).map((_: RecentCampaign, i: number) => (
                        <Cell key={i} fill={i % 2 === 0 ? '#0E9F84' : '#6D28D9'} />
                      ))}
                    </Bar>
                  </BarChart>
                </ResponsiveContainer>
              )}
            </>
          ) : (
            <div className="text-center py-8 text-ink/30 text-sm">No campaigns yet</div>
          )}
        </Section>

        {/* Recent Failures */}
        <Section title="Recent Failures">
          {failures.length === 0 ? (
            <div className="text-center py-8">
              <p className="text-3xl mb-3">✓</p>
              <p className="text-sm font-medium text-ink/60">No failures</p>
              <p className="text-xs text-ink/40 mt-1">All notifications delivered successfully</p>
            </div>
          ) : (
            <div className="space-y-3">
              {failures.slice(0, 5).map((f: FailureDto, i: number) => (
                <div key={i} className="bg-white border border-ink/10 rounded-lg p-3">
                  <div className="flex items-center justify-between mb-1">
                    {/* title = notification type (e.g. "PasswordReset") */}
                    <span className="text-xs font-medium text-coral">{f.title}</span>
                    <span className="text-xs text-ink/40">
                      {new Date(f.occurredAt).toLocaleString()}
                    </span>
                  </div>
                  {/* reason = raw provider response */}
                  <p className="text-xs text-ink/60 font-mono truncate">{f.reason}</p>
                  <div className="flex items-center gap-3 mt-1">
                    <span className="text-xs text-ink/40">Provider: {f.provider}</span>
                    <span className="text-xs text-ink/40">Retries: {f.retryCount}</span>
                    {f.suggestedAction && (
                      <span className="text-xs text-violet truncate">{f.suggestedAction}</span>
                    )}
                  </div>
                </div>
              ))}
            </div>
          )}
        </Section>
      </div>
    </AppLayout>
  )
}