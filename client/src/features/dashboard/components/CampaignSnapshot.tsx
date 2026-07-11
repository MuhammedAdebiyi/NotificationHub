import { useNavigate } from 'react-router-dom'
import type { CampaignSnapshot, RecentCampaign } from '../types'

interface Props {
  data: CampaignSnapshot | null
}

const statusPill: Record<RecentCampaign['status'], string> = {
  Running:   'bg-violet/10 text-violet',
  Completed: 'bg-teal/10 text-teal',
  Scheduled: 'bg-fog border border-ink/10 text-ink/50',
  Draft:     'bg-yellow-50 border border-yellow-200 text-yellow-700',
}

const summaryConfig = [
  { key: 'running' as const,        label: 'Running',      accent: 'text-violet' },
  { key: 'scheduled' as const,      label: 'Scheduled',    accent: 'text-ink' },
  { key: 'drafts' as const,         label: 'Drafts',       accent: 'text-ink' },
  { key: 'completedToday' as const, label: 'Done today',   accent: 'text-teal' },
]

function fmtRecipients(n: number) {
  if (n >= 1_000_000) return `${(n / 1_000_000).toFixed(1)}M recipients`
  if (n >= 1000) return `${(n / 1000).toFixed(0)}k recipients`
  return `${n} recipients`
}

export default function CampaignSnapshot({ data }: Props) {
  const navigate = useNavigate()

  return (
    <div className="bg-white rounded-xl border border-ink/10 p-4 flex flex-col gap-4">
      <div className="flex items-center justify-between">
        <h3 className="text-sm font-semibold">Campaigns</h3>
        <button
          onClick={() => navigate('/campaigns')}
          className="text-[11px] text-violet hover:underline"
        >
          View all →
        </button>
      </div>

      {/* Summary tiles */}
      <div className="grid grid-cols-4 gap-2">
        {summaryConfig.map(({ key, label, accent }) => (
          <div
            key={key}
            className="text-center py-2.5 px-1 bg-fog rounded-lg cursor-pointer hover:bg-violet/5 transition-colors"
            onClick={() => navigate('/campaigns')}
          >
            <div className={`text-lg font-semibold leading-none ${accent}`}>
              {data?.[key] ?? '—'}
            </div>
            <div className="text-[10px] text-ink/40 mt-1">{label}</div>
          </div>
        ))}
      </div>

      {/* Recent campaign rows */}
      <div className="flex flex-col divide-y divide-ink/5">
        {(data?.recent ?? []).map((c) => (
          <div
            key={c.id}
            className="flex items-center justify-between py-2.5 cursor-pointer hover:bg-fog -mx-1 px-1 rounded transition-colors"
            onClick={() => navigate(`/campaigns/${c.id}`)}
          >
            <div className="flex flex-col gap-0.5 min-w-0">
              <span className="text-sm font-medium truncate">{c.title}</span>
              <span className="text-[11px] text-ink/40">
                {c.scheduledAt
                  ? `Scheduled ${c.scheduledAt}`
                  : fmtRecipients(c.recipientCount)}
              </span>
            </div>

            <div className="flex items-center gap-2 ml-3 shrink-0">
              {c.progressPercent != null && (
                <span className="text-sm font-medium">{c.progressPercent}%</span>
              )}
              <span className={`text-[10px] font-medium px-2 py-0.5 rounded-full ${statusPill[c.status]}`}>
                {c.status}
              </span>
            </div>
          </div>
        ))}

        {!data?.recent?.length && (
          <p className="text-xs text-ink/40 py-2 text-center">No recent campaigns</p>
        )}
      </div>
    </div>
  )
}