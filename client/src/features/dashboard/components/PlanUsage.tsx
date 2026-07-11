import type { TodayStats } from '../types'

interface Props {
  stats: TodayStats | null
}

export default function PlanUsage({ stats }: Props) {
  const pct = stats?.planUsagePct ?? 0

  const barColor =
    pct >= 90 ? 'bg-coral' :
    pct >= 70 ? 'bg-yellow-400' :
    'bg-violet'

  return (
    <div className="bg-white rounded-xl border border-ink/10 p-4 flex flex-col gap-3">
      <div className="flex items-center justify-between">
        <h3 className="text-sm font-semibold">Plan usage</h3>
        <span className="text-[11px] font-medium bg-fog border border-ink/10 text-ink/50 px-2 py-0.5 rounded-full">
          {stats?.plan ?? 'Free'}
        </span>
      </div>

      <div className="flex flex-col gap-2">
        <div className="flex justify-between items-baseline">
          <span className="text-[11px] text-ink/40">Capacity used</span>
          <span className="text-sm font-semibold">{Math.round(pct)}%</span>
        </div>

        <div className="h-1.5 bg-fog rounded-full overflow-hidden">
          <div
            className={`h-1.5 rounded-full transition-all duration-500 ${barColor}`}
            style={{ width: `${Math.min(pct, 100)}%` }}
          />
        </div>

        <div className="flex justify-between text-[11px] text-ink/40 mt-0.5">
          <span>{stats?.apiCallsToday?.toLocaleString() ?? '—'} API calls today</span>
          <span>{stats?.notificationsSent?.toLocaleString() ?? '—'} notifications</span>
        </div>
      </div>

      {pct >= 80 && (
        <div className="text-[11px] text-yellow-700 bg-yellow-50 border border-yellow-200 rounded-lg px-3 py-2">
          Approaching your plan limit. Consider upgrading.
        </div>
      )}
    </div>
  )
}