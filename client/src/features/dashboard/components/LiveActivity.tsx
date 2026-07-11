import type { ActivityItem } from '../types'

interface Props {
  data: ActivityItem[] | null
}

const iconConfig: Record<ActivityItem['type'], { bg: string; text: string; icon: React.ReactNode }> = {
  sent: {
    bg: 'bg-teal/10',
    text: 'text-teal',
    icon: (
      <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" aria-hidden="true">
        <polyline points="20 6 9 17 4 12"/>
      </svg>
    ),
  },
  failed: {
    bg: 'bg-coral/10',
    text: 'text-coral',
    icon: (
      <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" aria-hidden="true">
        <circle cx="12" cy="12" r="10"/><line x1="12" y1="8" x2="12" y2="12"/><line x1="12" y1="16" x2="12.01" y2="16"/>
      </svg>
    ),
  },
  retry: {
    bg: 'bg-yellow-50',
    text: 'text-yellow-600',
    icon: (
      <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" aria-hidden="true">
        <polyline points="1 4 1 10 7 10"/><path d="M3.51 15a9 9 0 1 0 .49-3.53"/>
      </svg>
    ),
  },
  campaign: {
    bg: 'bg-violet/10',
    text: 'text-violet',
    icon: (
      <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" aria-hidden="true">
        <polygon points="11 5 6 9 2 9 2 15 6 15 11 19 11 5"/><path d="M15.54 8.46a5 5 0 0 1 0 7.07"/>
      </svg>
    ),
  },
  key: {
    bg: 'bg-fog',
    text: 'text-ink/50',
    icon: (
      <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" aria-hidden="true">
        <path d="M21 2l-2 2m-7.61 7.61a5.5 5.5 0 1 1-7.778 7.778 5.5 5.5 0 0 1 7.777-7.777zm0 0L15.5 7.5m0 0l3 3L22 7l-3-3m-3.5 3.5L19 4"/>
      </svg>
    ),
  },
  invite: {
    bg: 'bg-fog',
    text: 'text-ink/50',
    icon: (
      <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" aria-hidden="true">
        <path d="M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2M20 8v6M23 11h-6M9 7a4 4 0 1 1-8 0 4 4 0 0 1 8 0"/>
      </svg>
    ),
  },
  dlq: {
    bg: 'bg-coral/10',
    text: 'text-coral',
    icon: (
      <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" aria-hidden="true">
        <path d="M10.29 3.86L1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.71 3.86a2 2 0 0 0-3.42 0z"/><line x1="12" y1="9" x2="12" y2="13"/><line x1="12" y1="17" x2="12.01" y2="17"/>
      </svg>
    ),
  },
}

export default function LiveActivity({ data }: Props) {
  return (
    <div className="bg-white rounded-xl border border-ink/10 p-4 flex flex-col gap-3">
      <div className="flex items-center justify-between">
        <h3 className="text-sm font-semibold">Live activity</h3>
        <span className="flex items-center gap-1.5 text-[11px] text-ink/40">
          <span className="w-1.5 h-1.5 rounded-full bg-teal animate-pulse" />
          Live
        </span>
      </div>

      <div className="flex flex-col divide-y divide-ink/5">
        {(data ?? []).map((item) => {
          const ic = iconConfig[item.type]
          return (
            <div key={item.id} className="flex items-start gap-2.5 py-2">
              <span className="text-[11px] text-ink/30 font-mono min-w-9.5 pt-0.5 tabular-nums">
                {item.time}
              </span>
              <div className={`w-6 h-6 rounded-md flex items-center justify-center shrink-0 ${ic.bg} ${ic.text}`}>
                {ic.icon}
              </div>
              <div className="flex flex-col gap-0 min-w-0 flex-1">
                <span className="text-sm font-medium leading-snug truncate">{item.title}</span>
                <span className="text-[11px] text-ink/40 truncate">{item.subtitle}</span>
              </div>
            </div>
          )
        })}

        {!data?.length && (
          <p className="text-xs text-ink/40 py-3 text-center">No activity yet</p>
        )}
      </div>
    </div>
  )
}