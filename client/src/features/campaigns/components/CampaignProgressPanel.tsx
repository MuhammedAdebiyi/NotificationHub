import { useState, useEffect, useRef } from 'react'
import { apiClient } from '@/shared/services/apiClient'

interface CampaignProgress {
  campaignId: string
  status: string
  total: number
  unqueued: number
  pending: number
  processing: number
  retrying: number
  sent: number
  failed: number
  deadLetter: number
  progressPercent: number
  startedAt: string | null
  completedAt: string | null
}

export default function CampaignProgressPanel({
  campaignId,
  isRunning,
}: {
  campaignId: string
  isRunning: boolean
}) {
  const [progress, setProgress] = useState<CampaignProgress | null>(null)
  const intervalRef = useRef<ReturnType<typeof setInterval> | null>(null)

  async function fetchProgress() {
    try {
      const data = await apiClient.get<CampaignProgress>(
        `/api/v1/campaigns/${campaignId}/progress`
      )
      setProgress(data)
    } catch {
      // fail silently
    }
  }

  useEffect(() => {
    fetchProgress()

    if (isRunning) {
      intervalRef.current = setInterval(fetchProgress, 3000)
    }

    return () => {
      if (intervalRef.current) clearInterval(intervalRef.current)
    }
  }, [campaignId, isRunning])

  if (!progress) return null

  const barColor = progress.failed + progress.deadLetter > 0 ? 'bg-coral' : 'bg-teal'

  return (
    <div className={`rounded-xl border p-5 mb-6 ${
      isRunning
        ? 'bg-yellow/5 border-yellow/20'
        : 'bg-teal/5 border-teal/20'
    }`}>
      {/* Header */}
      <div className="flex items-center justify-between mb-4">
        <div className="flex items-center gap-2">
          {isRunning && (
            <span className="w-2 h-2 rounded-full bg-yellow-400 animate-pulse" />
          )}
          <p className="text-sm font-semibold">
            {isRunning ? 'Live — sending in progress' : 'Delivery complete'}
          </p>
          {isRunning && (
            <span className="text-xs text-ink/40">updates every 3s</span>
          )}
        </div>
        <span className="text-sm font-bold text-violet">
          {progress.progressPercent}%
        </span>
      </div>

      {/* Progress bar */}
      <div className="w-full h-2 bg-ink/10 rounded-full mb-5 overflow-hidden">
        <div
          className={`h-2 rounded-full transition-all duration-500 ${barColor}`}
          style={{ width: `${progress.progressPercent}%` }}
        />
      </div>

      {/* Status grid */}
      <div className="grid grid-cols-3 sm:grid-cols-6 gap-3 text-center">
        <StatusCell label="Unqueued" value={progress.unqueued} color="text-ink/40" />
        <StatusCell label="Pending" value={progress.pending} color="text-violet" />
        <StatusCell label="Processing" value={progress.processing} color="text-yellow-600" />
        <StatusCell label="Retrying" value={progress.retrying} color="text-yellow-500" />
        <StatusCell label="Sent" value={progress.sent} color="text-teal" />
        <StatusCell
          label="Failed / DLQ"
          value={progress.failed + progress.deadLetter}
          color={progress.failed + progress.deadLetter > 0 ? 'text-coral' : 'text-ink/30'}
        />
      </div>

      {/* Timing */}
      {progress.startedAt && (
        <p className="text-xs text-ink/40 mt-4 text-center">
          Started {new Date(progress.startedAt).toLocaleString()}
          {progress.completedAt && (
            <> · Finished {new Date(progress.completedAt).toLocaleString()}</>
          )}
        </p>
      )}
    </div>
  )
}

function StatusCell({
  label,
  value,
  color,
}: {
  label: string
  value: number
  color: string
}) {
  return (
    <div>
      <p className={`font-display font-bold text-xl ${color}`}>{value}</p>
      <p className="text-xs text-ink/40 mt-0.5">{label}</p>
    </div>
  )
}