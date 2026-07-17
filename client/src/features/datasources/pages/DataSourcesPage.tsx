import { useState } from 'react'
import AppLayout from '@/app/layouts/AppLayout'
import { useDataSources } from '../hooks/useDataSources'
import { dataSourceApi } from '../api/dataSourceApi'
import { SQL_CAPABLE_TYPES, type DataSourceType } from '../types/dataSource.types'

function statusStyle(status: string) {
  const s = status.toLowerCase()
  if (s.includes('connect') || s.includes('success')) return 'bg-teal/10 text-teal'
  if (s.includes('fail') || s.includes('error')) return 'bg-coral/10 text-coral'
  return 'bg-ink/10 text-ink/50'
}

const emptyForm = {
  name: '',
  type: 'PostgreSql' as DataSourceType,
  connectionString: '',
  host: '',
  database: '',
}

export default function DataSourcesPage() {
  const { dataSources, totalCount, isLoading, error, reload } = useDataSources()
  const [showForm, setShowForm] = useState(false)
  const [form, setForm] = useState(emptyForm)
  const [creating, setCreating] = useState(false)
  const [createError, setCreateError] = useState<string | null>(null)

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

  return (
    <AppLayout>
      <div className="mb-8 flex items-center justify-between">
        <div>
          <p className="hand text-xl text-violet mb-1">connect a database —</p>
          <h1 className="font-display font-bold text-3xl">Data Sources</h1>
        </div>
        <button
          onClick={() => setShowForm(true)}
          className="px-4 py-2 bg-ink text-white rounded-lg text-sm font-medium hover:bg-violet transition"
        >
          + New Data Source
        </button>
      </div>

      {showForm && (
        <div className="bg-white border border-ink/10 rounded-xl p-6 mb-6">
          <div className="flex items-center justify-between mb-4">
            <h2 className="font-display font-bold text-lg">New Data Source</h2>
            <button
              type="button"
              onClick={() => { setShowForm(false); setCreateError(null) }}
              className="text-ink/40 hover:text-ink text-sm"
            >
              Cancel
            </button>
          </div>

          {createError && (
            <div className="bg-coral/10 text-coral text-sm px-4 py-3 rounded-xl mb-4">{createError}</div>
          )}

          <form onSubmit={handleCreate} className="space-y-4">
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
                  className="w-full border border-ink/20 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-violet bg-white"
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
                Encrypted at rest. We test the connection before saving — it's never stored if the test fails.
              </p>
            </div>

            <div className="grid sm:grid-cols-2 gap-4">
              <div>
                <label className="block text-sm font-medium mb-1">Host <span className="text-ink/30">(optional, for display)</span></label>
                <input
                  className="w-full border border-ink/20 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-violet"
                  value={form.host}
                  onChange={e => setForm(f => ({ ...f, host: e.target.value }))}
                />
              </div>
              <div>
                <label className="block text-sm font-medium mb-1">Database <span className="text-ink/30">(optional, for display)</span></label>
                <input
                  className="w-full border border-ink/20 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-violet"
                  value={form.database}
                  onChange={e => setForm(f => ({ ...f, database: e.target.value }))}
                />
              </div>
            </div>

            <div className="flex gap-3 pt-2">
              <button
                type="submit"
                disabled={creating || !form.name.trim() || !form.connectionString.trim()}
                className="px-4 py-2 bg-ink text-white rounded-lg text-sm font-medium hover:bg-violet transition disabled:opacity-50"
              >
                {creating ? 'Testing connection...' : 'Test & Save'}
              </button>
              <button
                type="button"
                onClick={() => { setShowForm(false); setCreateError(null) }}
                className="px-4 py-2 border border-ink/20 rounded-lg text-sm font-medium hover:bg-fog transition"
              >
                Cancel
              </button>
            </div>
          </form>
        </div>
      )}

      {isLoading ? (
        <div className="text-sm text-ink/40 py-10 text-center">Loading...</div>
      ) : error ? (
        <div className="text-sm text-coral py-10 text-center">{error}</div>
      ) : dataSources.length === 0 ? (
        <div className="text-center py-16 text-ink/40">
          <p className="text-lg font-medium mb-1">No data sources yet</p>
          <p className="text-sm">Connect a database to import recipients directly into campaigns.</p>
        </div>
      ) : (
        <div className="bg-fog border border-ink/10 rounded-xl overflow-hidden">
          <table className="w-full text-sm">
            <thead className="border-b border-ink/10">
              <tr className="text-left text-ink/50 text-xs uppercase tracking-wide">
                <th className="px-4 py-3 font-medium">Name</th>
                <th className="px-4 py-3 font-medium">Type</th>
                <th className="px-4 py-3 font-medium">Host / Database</th>
                <th className="px-4 py-3 font-medium">Status</th>
                <th className="px-4 py-3 font-medium">Created</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-ink/5">
              {dataSources.map(d => (
                <tr key={d.id} className="hover:bg-fog/50 transition">
                  <td className="px-4 py-3 font-medium">{d.name}</td>
                  <td className="px-4 py-3 text-ink/60">{d.type}</td>
                  <td className="px-4 py-3 text-ink/40 text-xs">
                    {d.host || '—'}{d.database ? ` / ${d.database}` : ''}
                  </td>
                  <td className="px-4 py-3">
                    <span className={`text-xs px-2 py-0.5 rounded-full font-medium ${statusStyle(d.status)}`}>
                      {d.status}
                    </span>
                  </td>
                  <td className="px-4 py-3 text-ink/40 text-xs">
                    {new Date(d.createdAt).toLocaleDateString()}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
          <div className="px-4 py-3 border-t border-ink/10 text-xs text-ink/40">
            {totalCount} data source{totalCount !== 1 ? 's' : ''} total
          </div>
        </div>
      )}
    </AppLayout>
  )
}