import { useState, useEffect } from 'react'
import AppLayout from '@/app/layouts/AppLayout'
import { apiClient } from '@/shared/services/apiClient'

interface ApiKey {
  id: string
  name: string
  keyPrefix: string
  isActive: boolean
  createdAt: string
}

export default function SettingsPage() {
  const [keys, setKeys] = useState<ApiKey[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [newKeyName, setNewKeyName] = useState('')
  const [creating, setCreating] = useState(false)
  const [newKeyValue, setNewKeyValue] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)

  async function load() {
    try {
      const res = await apiClient.get<{ items: ApiKey[] }>('/api/v1/org/api-keys')
      setKeys(res.items)
    } catch {
      // fail silently
    } finally {
      setIsLoading(false)
    }
  }

  useEffect(() => { load() }, [])

  async function handleCreate() {
    if (!newKeyName.trim()) return
    setCreating(true)
    setError(null)
    try {
      const res = await apiClient.post<{ key: string; id: string; name: string }>(
        '/api/v1/org/api-keys',
        { name: newKeyName }
      )
      setNewKeyValue(res.key)
      setNewKeyName('')
      await load()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to create key.')
    } finally {
      setCreating(false)
    }
  }

  async function handleRevoke(id: string) {
    if (!confirm('Revoke this API key? This cannot be undone.')) return
    try {
      await apiClient.delete(`/api/v1/org/api-keys/${id}`)
      await load()
    } catch {
      // fail silently
    }
  }

  return (
    <AppLayout>
      <div className="mb-8">
        <p className="hand text-xl text-violet mb-1">configuration —</p>
        <h1 className="font-display font-bold text-3xl">Settings</h1>
      </div>

      <div className="max-w-2xl space-y-8">
        {/* New key revealed */}
        {newKeyValue && (
          <div className="bg-teal/10 border border-teal/30 rounded-xl p-5">
            <p className="text-sm font-semibold text-teal mb-2">
              ✓ API key created — copy it now, it won't be shown again
            </p>
            <code className="block bg-white border border-teal/20 rounded-lg px-4 py-3 text-sm font-mono break-all">
              {newKeyValue}
            </code>
            <button
              onClick={() => { navigator.clipboard.writeText(newKeyValue); setNewKeyValue(null) }}
              className="mt-3 text-xs px-3 py-1.5 bg-teal text-white rounded-lg hover:bg-teal/80 transition"
            >
              Copy & dismiss
            </button>
          </div>
        )}

        {/* Create new key */}
        <div className="bg-fog border border-ink/10 rounded-xl p-6">
          <h2 className="font-display font-bold text-lg mb-4">API Keys</h2>
          <div className="flex gap-3 mb-6">
            <input
              className="flex-1 border border-ink/20 rounded-lg px-3 py-2 text-sm"
              placeholder="Key name e.g. CourseVault Production"
              value={newKeyName}
              onChange={e => setNewKeyName(e.target.value)}
              onKeyDown={e => e.key === 'Enter' && handleCreate()}
            />
            <button
              onClick={handleCreate}
              disabled={creating || !newKeyName.trim()}
              className="px-4 py-2 bg-ink text-white rounded-lg text-sm font-medium disabled:opacity-50 hover:bg-ink/80 transition"
            >
              {creating ? 'Creating...' : 'Create'}
            </button>
          </div>
          {error && <p className="text-sm text-coral mb-4">{error}</p>}

          {isLoading ? (
            <p className="text-sm text-ink/40">Loading...</p>
          ) : keys.length === 0 ? (
            <p className="text-sm text-ink/40 text-center py-6">No API keys yet.</p>
          ) : (
            <div className="divide-y divide-ink/5">
              {keys.map(k => (
                <div key={k.id} className="flex items-center justify-between py-3">
                  <div>
                    <p className="text-sm font-medium">{k.name}</p>
                    <p className="text-xs text-ink/40 font-mono mt-0.5">{k.keyPrefix}••••••••</p>
                  </div>
                  <div className="flex items-center gap-3">
                    <span className="text-xs text-ink/30">
                      {new Date(k.createdAt).toLocaleDateString()}
                    </span>
                    <button
                      onClick={() => handleRevoke(k.id)}
                      className="text-xs px-3 py-1 border border-coral/30 text-coral rounded-lg hover:bg-red-50 transition"
                    >
                      Revoke
                    </button>
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>
      </div>
    </AppLayout>
  )
}