import type { InfrastructureHealth, SystemStatus } from '../types'

interface Props {
  data: InfrastructureHealth | null
}

const statusStyles: Record<SystemStatus, { dot: string; label: string; text: string }> = {
  healthy:  { dot: 'bg-teal',  label: 'Healthy',  text: 'text-teal' },
  degraded: { dot: 'bg-yellow-500', label: 'Degraded', text: 'text-yellow-600' },
  down:     { dot: 'bg-coral', label: 'Down',     text: 'text-coral' },
}

const serviceIcons: Record<string, React.ReactNode> = {
  API: (
    <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" aria-hidden="true">
      <rect x="2" y="3" width="20" height="14" rx="2"/><path d="M8 21h8M12 17v4"/>
    </svg>
  ),
  Database: (
    <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" aria-hidden="true">
      <ellipse cx="12" cy="5" rx="9" ry="3"/><path d="M21 12c0 1.66-4 3-9 3s-9-1.34-9-3"/><path d="M3 5v14c0 1.66 4 3 9 3s9-1.34 9-3V5"/>
    </svg>
  ),
  Redis: (
    <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" aria-hidden="true">
      <rect x="2" y="2" width="20" height="8" rx="2"/><rect x="2" y="14" width="20" height="8" rx="2"/><line x1="6" y1="6" x2="6.01" y2="6"/><line x1="6" y1="18" x2="6.01" y2="18"/>
    </svg>
  ),
  Worker: (
    <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" aria-hidden="true">
      <rect x="4" y="4" width="16" height="16" rx="2"/><rect x="9" y="9" width="6" height="6"/><line x1="9" y1="1" x2="9" y2="4"/><line x1="15" y1="1" x2="15" y2="4"/><line x1="9" y1="20" x2="9" y2="23"/><line x1="15" y1="20" x2="15" y2="23"/><line x1="20" y1="9" x2="23" y2="9"/><line x1="20" y1="14" x2="23" y2="14"/><line x1="1" y1="9" x2="4" y2="9"/><line x1="1" y1="14" x2="4" y2="14"/>
    </svg>
  ),
  SendByte: (
    <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" aria-hidden="true">
      <path d="M4 4h16c1.1 0 2 .9 2 2v12c0 1.1-.9 2-2 2H4c-1.1 0-2-.9-2-2V6c0-1.1.9-2 2-2z"/><polyline points="22,6 12,13 2,6"/>
    </svg>
  ),
}

export default function InfrastructureStatus({ data }: Props) {
  const hasIssue = data?.services.some(s => s.status !== 'healthy')

  return (
    <div className="bg-white rounded-xl border border-ink/10 p-4 flex flex-col gap-3">
      <div className="flex items-center justify-between">
        <h3 className="text-sm font-semibold">Infrastructure</h3>
        {hasIssue ? (
          <span className="text-[11px] font-medium text-coral">Issue detected</span>
        ) : (
          <span className="text-[11px] text-teal">All healthy</span>
        )}
      </div>

      <div className="flex flex-col gap-2.5">
        {(data?.services ?? []).map((svc) => {
          const s = statusStyles[svc.status]
          return (
            <div key={svc.name} className="flex items-center justify-between">
              <span className="flex items-center gap-2 text-xs text-ink/60">
                <span className="text-ink/30">{serviceIcons[svc.name]}</span>
                {svc.name}
              </span>
              <span className={`flex items-center gap-1.5 text-[11px] font-medium ${s.text}`}>
                <span className={`w-1.5 h-1.5 rounded-full ${s.dot}`} />
                {svc.detail || s.label}
              </span>
            </div>
          )
        })}

        {!data?.services.length && (
          <p className="text-xs text-ink/40 text-center py-1">Loading...</p>
        )}
      </div>
    </div>
  )
}