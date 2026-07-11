import { useNavigate } from 'react-router-dom'
import type { RecentFailure } from '../types'

interface Props {
  data: RecentFailure[] | null
}

const failureTypeLabel: Record<RecentFailure['failureType'], string> = {
  smtp_timeout:  'SMTP timeout',
  rate_limited:  'Rate limited',
  invalid_email: 'Invalid email',
  dead_letter:   'Dead letter — 5 attempts exhausted',
  unknown:       'Unknown error',
}

const statusColor: Record<RecentFailure['status'], string> = {
  retrying:    'text-yellow-600',
  dead_letter: 'text-coral',
  failed:      'text-coral',
}

const statusLabel: Record<RecentFailure['status'], string> = {
  retrying:    'Retrying',
  dead_letter: 'Dead letter',
  failed:      'Failed',
}

function WarningIcon({ className }: { className?: string }) {
  return (
    <svg width="11" height="11" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" aria-hidden="true" className={className}>
      <path d="M10.29 3.86L1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.71 3.86a2 2 0 0 0-3.42 0z"/><line x1="12" y1="9" x2="12" y2="13"/><line x1="12" y1="17" x2="12.01" y2="17"/>
    </svg>
  )
}

export default function RecentFailures({ data }: Props) {
  const navigate = useNavigate()

  return (
    <div className="bg-white rounded-xl border border-ink/10 p-4 flex flex-col gap-3">
      <div className="flex items-center justify-between">
        <h3 className="text-sm font-semibold">Recent failures</h3>
        <button
          onClick={() => navigate('/notifications?status=Failed')}
          className="text-[11px] text-violet hover:underline"
        >
          View all →
        </button>
      </div>

      <div className="flex flex-col divide-y divide-ink/5">
        {(data ?? []).map((f) => (
          <div key={f.notificationId} className="flex items-center justify-between py-2.5">
            <div className="flex flex-col gap-0.5 min-w-0 mr-3">
              <span className="text-sm font-medium truncate">{f.title}</span>
              <span className={`text-[11px] flex items-center gap-1 ${statusColor[f.status]}`}>
                <WarningIcon />
                {failureTypeLabel[f.failureType]}
                {' — '}
                {statusLabel[f.status]}
              </span>
            </div>

            <button
              onClick={() => navigate(`/notifications/${f.notificationId}`)}
              className="text-[11px] font-medium text-violet border border-violet/20 px-2.5 py-1 rounded-md hover:bg-violet/5 transition-colors shrink-0"
            >
              {f.status === 'dead_letter' ? 'Review' : f.status === 'retrying' ? 'Inspect' : 'Retry'}
            </button>
          </div>
        ))}

        {!data?.length && (
          <p className="text-xs text-ink/40 py-2 text-center">No recent failures </p>
        )}
      </div>
    </div>
  )
}