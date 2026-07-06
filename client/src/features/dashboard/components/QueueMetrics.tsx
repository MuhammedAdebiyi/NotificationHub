import { useState, useEffect } from 'react'
import { apiClient } from '@/shared/services/apiClient'

interface VolumePoint {
  date: string
  count: number
}

export default function QueueMetrics() {
  const [data, setData] = useState<VolumePoint[]>([])
  const [isLoading, setIsLoading] = useState(true)

  useEffect(() => {
    apiClient.get<VolumePoint[]>('/api/v1/dashboard/volume')
      .then(setData)
      .catch(() => {})
      .finally(() => setIsLoading(false))
  }, [])

  const max = Math.max(...data.map(d => d.count), 1)

  return (
    <div className="lg:col-span-2 bg-fog border border-ink/10 rounded-lg p-6 min-h-[280px]">
      <h2 className="font-display font-bold text-lg mb-1">Notification Volume</h2>
      <p className="text-sm text-ink/50 mb-6">Last 7 days · all channels</p>

      {isLoading ? (
        <div className="h-48 flex items-center justify-center text-ink/30 text-sm">
          Loading...
        </div>
      ) : (
        <div className="flex items-end gap-2 h-48">
          {data.map(d => (
            <div key={d.date} className="flex-1 flex flex-col items-center gap-1">
              <span className="text-xs text-ink/40 font-mono">{d.count || ''}</span>
              <div
                className="w-full bg-violet/80 rounded-t-md transition-all"
                style={{ height: `${Math.max((d.count / max) * 160, d.count > 0 ? 8 : 2)}px` }}
              />
              <span className="text-xs text-ink/40 whitespace-nowrap">{d.date}</span>
            </div>
          ))}
        </div>
      )}
    </div>
  )
}