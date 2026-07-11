import type { HealthBannerData } from '../types'

interface Props { data: HealthBannerData | null }

const cfg = {
  healthy: {
    wrap:  'bg-teal/10 border-teal/30',
    dot:   'bg-teal',
    title: 'text-teal',
    label: 'All systems healthy',
  },
  warning: {
    wrap:  'bg-yellow-50 border-yellow-300',
    dot:   'bg-yellow-500',
    title: 'text-yellow-700',
    label: 'Attention needed',
  },
  critical: {
    wrap:  'bg-coral/10 border-coral/30',
    dot:   'bg-coral',
    title: 'text-coral',
    label: 'Critical',
  },
}

export default function HealthBanner({ data }: Props) {
  const c   = cfg[data?.status ?? 'healthy']
  const sr  = data?.successRate   ?? 99.93
  const lat = data?.queueLatencyMs ?? 184
  const wk  = data?.workersOnline  ?? 4

  return (
    <div className={`flex flex-wrap items-center justify-between gap-3 rounded-xl px-4 py-3.5 border ${c.wrap}`}>
      <div className="flex items-center gap-2.5">
        <span className={`w-2.5 h-2.5 rounded-full shrink-0 ${c.dot}`} />
        <span className={`text-sm font-semibold ${c.title}`}>
          {data?.incidentMessage ?? c.label}
        </span>
      </div>
      <div className="flex flex-wrap gap-4 text-xs text-ink/50">
        <span>{sr.toFixed(2)}% success rate</span>
        <span>Queue latency {lat}ms</span>
        <span>{wk} workers online</span>
        {(!data || data.status === 'healthy') && <span>No incidents</span>}
      </div>
    </div>
  )
}