import { AreaChart, Area, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer } from 'recharts'
import type { DeliveryPoint } from '../types'

interface Props { data: DeliveryPoint[] | null }

function formatLocalHour(isoTimestamp: string): string {
  const d = new Date(isoTimestamp)
  if (Number.isNaN(d.getTime())) return isoTimestamp
  return d.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit', hour12: false })
}

export default function DeliveryChart({ data }: Props) {
  const chartData = (data ?? []).map(d => ({
    ...d,
    hour: formatLocalHour(d.hour),
  }))

  return (
    <div className="bg-white rounded-xl border border-ink/10 p-4">
      <div className="flex items-center justify-between mb-3">
        <h3 className="text-sm font-semibold">Delivery volume — last 24h</h3>
        <span className="text-[11px] text-ink/40">every 5s</span>
      </div>

      <div className="flex gap-4 mb-3">
        {[
          { key: 'sent', label: 'Sent', color: '#7C3AED' },
          { key: 'failed', label: 'Failed', color: '#F87171' },
          { key: 'retrying', label: 'Retrying', color: '#FBBF24' },
        ].map(s => (
          <span key={s.key} className="flex items-center gap-1.5 text-xs text-ink/50">
            <span style={{ display: 'inline-block', width: 20, height: 2, background: s.color, borderRadius: 1 }} />
            {s.label}
          </span>
        ))}
      </div>

      <div style={{ height: 160 }}>
        {chartData.length > 0 ? (
          <ResponsiveContainer width="100%" height="100%">
            <AreaChart data={chartData}>
              <defs>
                <linearGradient id="sentGrad" x1="0" y1="0" x2="0" y2="1">
                  <stop offset="5%" stopColor="#7C3AED" stopOpacity={0.3} />
                  <stop offset="95%" stopColor="#7C3AED" stopOpacity={0} />
                </linearGradient>
              </defs>
              <CartesianGrid strokeDasharray="3 3" stroke="#f3f4f6" />
              <XAxis
                dataKey="hour"
                tick={{ fontSize: 10, fill: '#9ca3af' }}
                tickLine={false}
                axisLine={false}
                interval={3}
              />
              <YAxis
                tick={{ fontSize: 10, fill: '#9ca3af' }}
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
              <Area type="monotone" dataKey="sent" stroke="#7C3AED" strokeWidth={2} fill="url(#sentGrad)" name="Sent" />
              <Area type="monotone" dataKey="failed" stroke="#F87171" strokeWidth={1.5} fill="none" name="Failed" strokeDasharray="4 3" />
              <Area type="monotone" dataKey="retrying" stroke="#FBBF24" strokeWidth={1.5} fill="none" name="Retrying" strokeDasharray="2 2" />
            </AreaChart>
          </ResponsiveContainer>
        ) : (
          <div className="h-full flex items-center justify-center text-ink/30 text-sm border border-dashed border-ink/15 rounded-lg">
            No data yet
          </div>
        )}
      </div>
    </div>
  )
}
