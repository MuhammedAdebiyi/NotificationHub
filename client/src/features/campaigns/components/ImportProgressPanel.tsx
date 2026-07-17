import { useState, useEffect, useRef } from 'react'
import { importApi } from '../api/importApi'
import type { ImportJob } from '../types/import.types'

export default function ImportProgressPanel({
  campaignId,
  importJobId,
  onComplete,
}: {
  campaignId: string
  importJobId: string
  onComplete: () => void
}) {
  const [job, setJob] = useState<ImportJob | null>(null)
  const intervalRef = useRef<ReturnType<typeof setInterval> | null>(null)
  const completedRef = useRef(false)

  async function poll() {
    try {
      const data = await importApi.getById(campaignId, importJobId)
      setJob(data)
      if (data.status === 'Completed' || data.status === 'Failed') {
        if (intervalRef.current) clearInterval(intervalRef.current)
        if (!completedRef.current) {
          completedRef.current = true
          onComplete()
        }
      }
    } catch {
      // fail silently, keep polling
    }
  }

  useEffect(() => {
    completedRef.current = false
    poll()
    intervalRef.current = setInterval(poll, 2000)
    return () => {
      if (intervalRef.current) clearInterval(intervalRef.current)
    }
  }, [importJobId])

  if (!job) return null

  const isRunning = job.status === 'Pending' || job.status === 'Running'
  const isFailed = job.status === 'Failed'

  return (
    <div className={`rounded-xl border p-5 mt-4 ${
      isFailed ? 'bg-coral/5 border-coral/20' : isRunning ? 'bg-yellow/5 border-yellow/20' : 'bg-teal/5 border-teal/20'
    }`}>
      <div className="flex items-center gap-2 mb-3">
        {isRunning && <span className="w-2 h-2 rounded-full bg-yellow-400 animate-pulse" />}
        <p className="text-sm font-semibold">
          {job.status === 'Pending' && 'Import queued...'}
          {job.status === 'Running' && 'Importing from database...'}
          {job.status === 'Completed' && '✓ Import complete'}
          {job.status === 'Failed' && '✕ Import failed'}
        </p>
      </div>

      <div className="grid grid-cols-3 gap-3 text-center mb-2">
        <div>
          <p className="font-display font-bold text-xl text-ink">{job.rowsRead.toLocaleString()}</p>
          <p className="text-xs text-ink/40 mt-0.5">Rows Read</p>
        </div>
        <div>
          <p className="font-display font-bold text-xl text-teal">{job.recipientsAdded.toLocaleString()}</p>
          <p className="text-xs text-ink/40 mt-0.5">Recipients Added</p>
        </div>
        <div>
          <p className={`font-display font-bold text-xl ${job.errorCount > 0 ? 'text-coral' : 'text-ink/30'}`}>
            {job.errorCount}
          </p>
          <p className="text-xs text-ink/40 mt-0.5">Errors</p>
        </div>
      </div>

      {job.lastError && (
        <p className="text-xs text-coral/80 mt-2 font-mono break-words">{job.lastError}</p>
      )}
    </div>
  )
}