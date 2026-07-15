import { useEffect, useRef } from 'react'
import { Chart, LineController, LineElement, PointElement, LinearScale, CategoryScale, Filler, Tooltip } from 'chart.js'
import type { DeliveryPoint } from '../types'

Chart.register(LineController, LineElement, PointElement, LinearScale, CategoryScale, Filler, Tooltip)

interface Props { data: DeliveryPoint[] | null }

const SERIES = [
  { key: 'sent'     as const, label: 'Sent',     color: '#7C3AED', dash: []    },
  { key: 'failed'   as const, label: 'Failed',   color: '#F87171', dash: [4,3] },
  { key: 'retrying' as const, label: 'Retrying', color: '#FBBF24', dash: [2,2] },
]


function formatLocalHour(isoTimestamp: string): string {
  const d = new Date(isoTimestamp)
  if (Number.isNaN(d.getTime())) return isoTimestamp // defensive fallback, shouldn't happen
  return d.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit', hour12: false })
}

export default function DeliveryChart({ data }: Props) {
const canvasRef = useRef<HTMLCanvasElement>(null)
const chartRef  = useRef<Chart | null>(null)

useEffect(() => {
if (!canvasRef.current) return

const labels   = data?.map(d => formatLocalHour(d.hour)) ?? []
const datasets = SERIES.map(s => ({
label:           s.label,
data:            data?.map(d => d[s.key]) ?? [],
borderColor:     s.color,
backgroundColor: s.key === 'sent' ? `${s.color}10` : 'transparent',
borderWidth:     s.key === 'sent' ? 2 : 1.5,
borderDash:      s.dash,
pointRadius:     0,
tension:         0.4,
fill:            s.key === 'sent',
    }))

if (chartRef.current) {
chartRef.current.data.labels = labels
chartRef.current.data.datasets.forEach((ds, i) => { ds.data = datasets[i].data })
chartRef.current.update('none')
return
    }

chartRef.current = new Chart(canvasRef.current, {
type: 'line',
data: { labels, datasets },
options: {
responsive: true,
maintainAspectRatio: false,
interaction: { mode: 'index', intersect: false },
plugins: {
legend: { display: false },
tooltip: {
backgroundColor: '#fff',
titleColor: '#1a1a2e',
bodyColor: '#6b7280',
borderColor: '#e5e7eb',
borderWidth: 1,
padding: 10,
callbacks: {
label: (ctx) => {
const value = ctx.parsed.y ?? 0
return ` ${ctx.dataset.label}: ${value.toLocaleString()}`
        },
        },
          },
        },
scales: {
x: { grid: { display: false }, ticks: { color: '#9ca3af', font: { size: 10 }, maxTicksLimit: 8, maxRotation: 0 }, border: { display: false } },
y: { grid: { color: '#f3f4f6', lineWidth: 0.5 }, ticks: { color: '#9ca3af', font: { size: 10 }, maxTicksLimit: 5 }, border: { display: false } },
        },
      },
    })

return () => { chartRef.current?.destroy(); chartRef.current = null }
  }, [data])

return (
<div className="bg-white rounded-xl border border-ink/10 p-4">
<div className="flex items-center justify-between mb-3">
<h3 className="text-sm font-semibold">Delivery volume — last 24h</h3>
<span className="text-[11px] text-ink/40">every 5s</span>
</div>

<div className="flex gap-4 mb-3">
{SERIES.map(s => (
<span key={s.key} className="flex items-center gap-1.5 text-xs text-ink/50">
<span style={{ display: 'inline-block', width: 20, height: 2, background: s.color, borderRadius: 1,
...(s.dash.length ? { backgroundImage: `repeating-linear-gradient(90deg,${s.color} 0 4px,transparent 4px 7px)`, background: 'none' } : {}) }} />
{s.label}
</span>
        ))}
</div>

<div style={{ position: 'relative', height: 160 }}>
<canvas ref={canvasRef} role="img" aria-label="Delivery volume over last 24 hours">
          Line chart: sent, failed, retrying notifications per hour.
</canvas>
</div>
</div>
  )
}