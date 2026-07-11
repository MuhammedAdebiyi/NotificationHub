import type { QueueStats } from '../types'

interface Props { data: QueueStats | null }

export default function QueueMonitor({ data: d }: Props) {
  const pendingPct = d && d.pending > 0
    ? Math.round((d.pending / (d.pending + d.processing + d.retrying + 1)) * 100)
    : 0

  return (
    <div className="bg-white rounded-xl border border-ink/10 p-4 flex flex-col gap-4">
      <h3 className="text-sm font-semibold">Queue monitor</h3>

      <div className="grid grid-cols-2 gap-3">
        <div className="flex flex-col gap-0.5">
          <span className="text-[11px] text-ink/40">Pending</span>
          <span className="text-lg font-bold font-display">{d?.pending?.toLocaleString() ?? '—'}</span>
          <div className="h-1 bg-fog rounded-full overflow-hidden mt-1">
            <div className="h-1 rounded-full bg-violet transition-all duration-500" style={{ width: `${pendingPct}%` }} />
          </div>
        </div>

        <div className="flex flex-col gap-0.5">
          <span className="text-[11px] text-ink/40">Processing</span>
          <span className="text-lg font-bold font-display text-teal">{d?.processing?.toLocaleString() ?? '—'}</span>
        </div>

        <div className="flex flex-col gap-0.5">
          <span className="text-[11px] text-ink/40">Retrying</span>
          <span className="text-lg font-bold font-display text-yellow-600">{d?.retrying?.toLocaleString() ?? '—'}</span>
        </div>

        <div className="flex flex-col gap-0.5">
          <span className="text-[11px] text-ink/40">Dead letter</span>
          <span className={`text-lg font-bold font-display ${d && d.deadLetter > 0 ? 'text-coral' : ''}`}>
            {d?.deadLetter?.toLocaleString() ?? '—'}
          </span>
        </div>
      </div>

      <div className="border-t border-ink/5 pt-3 flex flex-col gap-2">
        {[
          { label: 'Avg wait',   value: d ? `${d.avgWaitMs}ms` : '—' },
          { label: 'Workers',    value: d ? `${d.workersActive} / ${d.workerCapacity} slots` : '—' },
          { label: 'Throughput', value: d ? `~${d.throughputPerMin.toLocaleString()}/min` : '—' },
        ].map(({ label, value }) => (
          <div key={label} className="flex justify-between text-xs">
            <span className="text-ink/40">{label}</span>
            <span className="font-medium">{value}</span>
          </div>
        ))}
      </div>
    </div>
  )
}