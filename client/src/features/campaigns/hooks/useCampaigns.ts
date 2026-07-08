import { useState, useEffect, useCallback } from 'react'
import { campaignApi } from '../api/campaignApi'
import type { Campaign } from '../types/campaign.types'

export function useCampaigns() {
  const [campaigns, setCampaigns] = useState<Campaign[]>([])
  const [totalCount, setTotalCount] = useState(0)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const load = useCallback(async () => {
    setIsLoading(true)
    try {
      const res = await campaignApi.getAll()
      setCampaigns(res.items)
      setTotalCount(res.totalCount)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load campaigns.')
    } finally {
      setIsLoading(false)
    }
  }, [])

  useEffect(() => { load() }, [load])

  return { campaigns, totalCount, isLoading, error, reload: load }
}