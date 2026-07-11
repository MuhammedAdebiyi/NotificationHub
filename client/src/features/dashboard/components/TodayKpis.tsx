import type { TodayStats } from '../types'

interface Props { stats: TodayStats | null }

function fmt(n?: number) {
  if (n == null) return '—'
  if (n >= 1_000_000) return `${(n / 1_000_000).toFixed(1)}M`
  if (n >= 1_000)     return `${(n / 1_000).toFixed(1)}k`
  return n.toLocaleString()
}

interface CardProps {
  label: string
  value: string | number
  sub?: React.ReactNode
  badge?: string
  badgeColor?: string
}

function Card({ label, value, sub, badge, badgeColor = 'bg-fog border border-ink/10 text-ink/50' }: CardProps) {
  return (
    <div className="bg-fog rounded-xl p-3.5 flex flex-col gap-1">
      <span className="text-[11px] text-ink/40">{label}</span>
      <span className="text-[22px] font-bold leading-none font-display">{value}</span>
      {sub   && <span className="text-[11px] text-ink/50 mt-0.5">{sub}</span>}
      {badge && <span className={`text-[10px] font-medium px-1.5 py-0.5 rounded w-fit mt-0.5 ${badgeColor}`}>{badge}</span>}
    </div>
  )
}

function Delta({ n }: { n?: number }) {
  if (n == null) return null
  const up = n >= 0
  return (
    <span className={up ? 'text-teal' : 'text-coral'}>
      {up ? '↑' : '↓'} {up ? '+' : ''}{n.toFixed(1)}% vs yesterday
    </span>
  )
}

export default function TodayKpis({ stats: s }: Props) {
  return (
    <div>
      <p className="text-[11px] font-medium text-ink/40 uppercase tracking-wide mb-2">Today</p>
      <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-6 gap-2.5">

        <Card
          label="Notifications"
          value={fmt(s?.notificationsSent)}
          sub={<Delta n={s?.notificationsSentDelta} />}
        />

        <Card
          label="Success rate"
          value={s?.successRate != null ? `${s.successRate.toFixed(2)}%` : '—'}
          sub={<Delta n={s?.successRateDelta} />}
        />

        <Card
          label="Queue depth"
          value={fmt(s?.queueDepth)}
          sub={`${s?.workersActive ?? '—'} workers active`}
        />

        <Card
          label="Avg send time"
          value={s?.avgSendTimeMs != null ? `${s.avgSendTimeMs}ms` : '—'}
          sub={`p95: ${s?.p95SendTimeMs ?? '—'}ms`}
        />

        <Card
          label="Dead letters"
          value={s?.deadLetters ?? '—'}
          sub={
            s?.deadLettersNeedingReview
              ? <span className="text-coral">⚠ {s.deadLettersNeedingReview} need review</span>
              : 'None pending'
          }
        />

        <Card
          label="Campaigns running"
          value={s?.campaignsRunning ?? '—'}
          badge={s?.campaignsScheduled != null ? `${s.campaignsScheduled} scheduled` : undefined}
        />

      </div>
    </div>
  )
}