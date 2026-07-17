import { useState, useEffect, useCallback } from 'react'
import { dataSourceApi } from '../api/dataSourceApi'
import type { DataSource } from '../types/dataSource.types'

export function useDataSources() {
  const [dataSources, setDataSources] = useState<DataSource[]>([])
  const [totalCount, setTotalCount] = useState(0)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const load = useCallback(async () => {
    setIsLoading(true)
    try {
      const res = await dataSourceApi.getAll()
      setDataSources(res.items)
      setTotalCount(res.totalCount)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load data sources.')
    } finally {
      setIsLoading(false)
    }
  }, [])

  useEffect(() => { load() }, [load])

  return { dataSources, totalCount, isLoading, error, reload: load }
}