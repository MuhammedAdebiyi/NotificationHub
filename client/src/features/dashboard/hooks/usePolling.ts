import { useState, useEffect, useRef, useCallback } from 'react'

interface Options {
  intervalMs?: number
  enabled?: boolean
}

interface Result<T> {
  data: T | null
  loading: boolean
  error: string | null
  refresh: () => void
}

export function usePolling<T>(
  fetcher: () => Promise<T>,
  { intervalMs = 5000, enabled = true }: Options = {}
): Result<T> {
  const [data, setData]       = useState<T | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError]     = useState<string | null>(null)
  const timerRef   = useRef<ReturnType<typeof setInterval> | null>(null)
  const mountedRef = useRef(true)

  const run = useCallback(async () => {
    try {
      const result = await fetcher()
      if (mountedRef.current) { setData(result); setError(null) }
    } catch (e) {
      if (mountedRef.current) setError(e instanceof Error ? e.message : 'Unknown error')
    } finally {
      if (mountedRef.current) setLoading(false)
    }
  }, [fetcher])

  useEffect(() => {
    if (!enabled) return
    mountedRef.current = true
    run()
    timerRef.current = setInterval(run, intervalMs)
    return () => {
      mountedRef.current = false
      if (timerRef.current) clearInterval(timerRef.current)
    }
  }, [run, intervalMs, enabled])

  return { data, loading, error, refresh: run }
}