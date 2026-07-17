import { useState } from 'react'
import { useDataSources } from '@/features/datasources/hooks/useDataSources'
import { dataSourceApi } from '@/features/datasources/api/dataSourceApi'
import { SQL_CAPABLE_TYPES, type DataSourceType } from '@/features/datasources/types/dataSource.types'

function statusStyle(status: string) {
  const s = status.toLowerCase()
  if (s.includes('connect') || s.includes('success')) return 'bg-teal/10 text-teal'
  if (s.includes('fail') || s.includes('error')) return 'bg-coral/10 text-coral'
  return 'bg-ink/10 text-ink/50'
}

function isFailedStatus(status: string) {
  const s = status.toLowerCase()
  return s.includes('fail') || s.includes('error')
}

const emptyForm = {
  name: '',
  type: 'PostgreSql' as DataSourceType,
  connectionString: '',
  host: '',
  database: '',
}

export default function DataSourcesSection() {
  const { dataSources, isLoading, error, reload } = useDataSources()
  const [showForm, setShowForm] = useState(false)
  const [form, setForm] = useState(emptyForm)
  const [creating, setCreating] = useState(false)
  const [createError, setCreateError] = useState<string | null>(null)
  const [deleteTarget, setDeleteTarget] = useState<{ id: string; name: string } | null>(null)
  const [deleteError, setDeleteError] = useState<string | null>(null)

  async function handleCreate(e: React.FormEvent) {
    e.preventDefault()
    setCreating(true)
    setCreateError(null)
    try {
      await dataSourceApi.create({
        name: form.name,
        type: form.type,
        connectionString: form.connectionString,
        host: form.host || undefined,
        database: form.database || undefined,
      })
      setShowForm(false)
      setForm(emptyForm)
      await reload()
    } catch (err) {
      setCreateError(err instanceof Error ? err.message : 'Failed to connect. Check the connection string.')
    } finally {
      setCreating(false)
    }
  }

  async function confirmDelete() {
    if (!deleteTarget) return
    setDeleteError(null)
    try {
      await dataSourceApi.delete(deleteTarget.id)
      setDeleteTarget(null)
      await reload()
    } catch (err) {
      setDeleteError(err instanceof Error ? err.message : 'Failed to delete.')
      setDeleteTarget(null)
    }
  }

  return (
    <div className="bg-white border border-ink/10 rounded-xl p-6">
      {deleteTarget && (
        <div className="fixed inset-0 z-50 flex items-center justify-center">
          <div className="absolute inset-0 bg-ink/40 backdrop-blur-sm" onClick={() => setDeleteTarget(null)} />
          <div className="relative bg-white rounded-2xl p-8 max-w-sm w-full mx-4 shadow-2xl">
            <div className="w-12 h-12 bg-coral/10 rounded-full flex items-center justify-center mb-4">
              <span className="text-coral text-xl">⊘</span>
            </div>
            <h3 className="font-display font-bold text-lg mb-1">Delete data source?</h3>
            <p className="text-sm text-ink/60 mb-6">
              <span className="font-medium text-ink">"{deleteTarget.name}"</span> will be permanently removed.
              This cannot be undone.
            </p>
            <div className="flex gap-3">
              <button
                onClick={confirmDelete}
                className="flex-1 px-4 py-2.5 bg-coral text-white rounded-lg text-sm font-medium hover:bg-coral/80 transition"
              >
                Delete
              </button>
              <button
                onClick={() => setDeleteTarget(null)}
                className="flex-1 px-4 py-2.5 border border-ink/20 rounded-lg text-sm font-medium hover:bg-fog transition"
              >
                Cancel
              </button>
            </div>
          </div>
        </div>
      )}

      <div className="flex items-center justify-between mb-1">
        <h2 className="font-display font-bold text-lg">Data Sources</h2>
        <button
          onClick={() => setShowForm(s => !s)}
          className="text-xs px-3 py-1.5 bg-ink text-white rounded-lg hover:bg-violet transition"
        >
          {showForm ? 'Cancel' : '+ Add Data Source'}
        </button>
      </div>
      <p className="text-xs text-ink/40 mb-6">
        Connect external databases to import recipients directly into campaigns.
      </p>

      {deleteError && (
        <div className="bg-coral/10 text-coral text-sm px-4 py-3 rounded-xl mb-4">{deleteError}</div>
      )}

      {showForm && (
        <form onSubmit={handleCreate} className="space-y-4 mb-6 bg-fog/40 border border-ink/10 rounded-xl p-5">
          {createError && (
            <div className="bg-coral/10 text-coral text-sm px-4 py-3 rounded-xl">{createError}</div>
          )}

          <div className="grid sm:grid-cols-2 gap-4">
            <div>
              <label className="block text-sm font-medium mb-1">Name</label>
              <input
                className="w-full border border-ink/20 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-violet"
                placeholder="e.g. Customer DB"
                value={form.name}
                onChange={e => setForm(f => ({ ...f, name: e.target.value }))}
                required
              />
            </div>
            <div>
              <label className="block text-sm font-medium mb-1">Type</label>
              <select
                className="w-full border border-ink/20 rounded-lg px-3 py-2 text-sm bg-white focus:outline-none focus:ring-2 focus:ring-violet"
                value={form.type}
                onChange={e => setForm(f => ({ ...f, type: e.target.value as DataSourceType }))}
              >
                {SQL_CAPABLE_TYPES.map(t => (
                  <option key={t} value={t}>{t}</option>
                ))}
              </select>
            </div>
          </div>

          <div>
            <label className="block text-sm font-medium mb-1">Connection String</label>
            <input
              type="password"
              className="w-full border border-ink/20 rounded-lg px-3 py-2 text-sm font-mono focus:outline-none focus:ring-2 focus:ring-violet"
              placeholder="postgresql://user:pass@host:5432/db"
              value={form.connectionString}
              onChange={e => setForm(f => ({ ...f, connectionString: e.target.value }))}
              required
            />
            <p className="text-xs text-ink/40 mt-1">
              Encrypted at rest. Tested before saving — never stored if the test fails.
            </p>
          </div>

          <div className="grid sm:grid-cols-2 gap-4">
            <div>
              <label className="block text-sm font-medium mb-1">Host <span className="text-ink/30">(optional)</span></label>
              <input
                className="w-full border border-ink/20 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-violet"
                value={form.host}
                onChange={e => setForm(f => ({ ...f, host: e.target.value }))}
              />
            </div>
            <div>
              <label className="block text-sm font-medium mb-1">Database <span className="text-ink/30">(optional)</span></label>
              <input
                className="w-full border border-ink/20 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-violet"
                value={form.database}
                onChange={e => setForm(f => ({ ...f, database: e.target.value }))}
              />
            </div>
          </div>

          <button
            type="submit"
            disabled={creating || !form.name.trim() || !form.connectionString.trim()}
            className="px-4 py-2 bg-ink text-white rounded-lg text-sm font-medium hover:bg-violet transition disabled:opacity-50"
          >
            {creating ? 'Testing connection...' : 'Test & Save'}
          </button>
        </form>
      )}

      {isLoading ? (
        <p className="text-sm text-ink/40 text-center py-6">Loading...</p>
      ) : error ? (
        <p className="text-sm text-coral text-center py-6">{error}</p>
      ) : dataSources.length === 0 ? (
        <div className="text-center py-8 text-ink/40">
          <p className="text-sm">No data sources yet.</p>
          <p className="text-xs mt-1">Add one above to import recipients from an external database.</p>
        </div>
      ) : (
        <div className="divide-y divide-ink/5">
          {dataSources.map(d => (
            <div key={d.id} className="flex items-center justify-between py-3.5">
              <div>
                <p className="text-sm font-medium">{d.name}</p>
                <p className="text-xs text-ink/40 mt-0.5">
                  {d.type} · {d.host || '—'}{d.database ? ` / ${d.database}` : ''}
                </p>
                {d.lastError && isFailedStatus(d.status) && (
                  <p className="text-xs text-coral/70 mt-0.5 font-mono">{d.lastError}</p>
                )}
              </div>
              <div className="flex items-center gap-3">
                <span className={`text-xs px-2 py-0.5 rounded-full font-medium ${statusStyle(d.status)}`}>
                  {d.status}
                </span>
                <button
                  onClick={() => setDeleteTarget({ id: d.id, name: d.name })}
                  className="text-xs px-3 py-1.5 border border-coral/30 text-coral rounded-lg hover:bg-red-50 transition"
                >
                  Delete
                </button>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  )
}