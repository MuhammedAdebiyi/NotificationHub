import { useState, useEffect } from 'react'
import { useDataSources } from '@/features/datasources/hooks/useDataSources'
import { dataSourceApi } from '@/features/datasources/api/dataSourceApi'
import { importApi } from '../api/importApi'
import type { ImportJob } from '../types/import.types'
import type { ColumnInfo } from '@/features/datasources/types/dataSource.types'

function isUsable(status: string) {
  const s = status.toLowerCase()
  return !s.includes('fail') && !s.includes('error')
}

export default function ImportRecipientsPanel({
  campaignId,
  onImportStarted,
}: {
  campaignId: string
  onImportStarted: (job: ImportJob) => void
}) {
  const { dataSources, isLoading: loadingSources, reload } = useDataSources()
  const usableSources = dataSources.filter(ds => isUsable(ds.status))
  const failedCount = dataSources.length - usableSources.length

  const [dataSourceId, setDataSourceId] = useState('')
  const [tables, setTables] = useState<string[]>([])
  const [loadingTables, setLoadingTables] = useState(false)
  const [tableName, setTableName] = useState('')
  const [columns, setColumns] = useState<ColumnInfo[]>([])
  const [loadingColumns, setLoadingColumns] = useState(false)

  const [primaryKeyColumn, setPrimaryKeyColumn] = useState('')
  const [emailColumn, setEmailColumn] = useState('')
  const [firstNameColumn, setFirstNameColumn] = useState('')
  const [lastNameColumn, setLastNameColumn] = useState('')
  const [whereClause, setWhereClause] = useState('')

  const [submitting, setSubmitting] = useState(false)
  const [formError, setFormError] = useState<string | null>(null)

  useEffect(() => {
    setTables([])
    setTableName('')
    setColumns([])
    setPrimaryKeyColumn('')
    setEmailColumn('')
    setFirstNameColumn('')
    setLastNameColumn('')

    if (!dataSourceId) return

    setLoadingTables(true)
    dataSourceApi.getTables(dataSourceId)
      .then(res => setTables(res.tables))
      .catch(err => setFormError(err instanceof Error ? err.message : 'Failed to load tables.'))
      .finally(() => setLoadingTables(false))
  }, [dataSourceId])

  useEffect(() => {
    setColumns([])
    setPrimaryKeyColumn('')
    setEmailColumn('')
    setFirstNameColumn('')
    setLastNameColumn('')

    if (!dataSourceId || !tableName) return

    setLoadingColumns(true)
    dataSourceApi.getColumns(dataSourceId, tableName)
      .then(res => setColumns(res.columns))
      .catch(err => setFormError(err instanceof Error ? err.message : 'Failed to load columns.'))
      .finally(() => setLoadingColumns(false))
  }, [dataSourceId, tableName])

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setFormError(null)
    setSubmitting(true)
    try {
      const job = await importApi.create(campaignId, {
        dataSourceId,
        tableName,
        primaryKeyColumn,
        emailColumn,
        firstNameColumn: firstNameColumn || undefined,
        lastNameColumn: lastNameColumn || undefined,
        whereClause: whereClause || undefined,
      })
      onImportStarted(job)
    } catch (err) {
      setFormError(err instanceof Error ? err.message : 'Failed to start import.')
    } finally {
      setSubmitting(false)
    }
  }

  const canSubmit = dataSourceId && tableName && primaryKeyColumn && emailColumn

  if (!loadingSources && usableSources.length === 0) {
    return (
      <div className="border-2 border-dashed border-ink/15 rounded-xl p-8 text-center text-ink/40 text-sm">
        {dataSources.length === 0 ? (
          <>No data sources connected yet — </>
        ) : (
          <>All {dataSources.length} connected data source{dataSources.length !== 1 ? 's' : ''} failed to connect — </>
        )}
        <a href="/settings" className="text-violet underline">manage data sources</a>
      </div>
    )
  }

  return (
    <form onSubmit={handleSubmit} className="space-y-4">
      {formError && (
        <div className="bg-coral/10 text-coral text-sm px-4 py-3 rounded-xl">{formError}</div>
      )}

      <div>
        <div className="flex items-center justify-between mb-1">
          <label className="block text-sm font-medium">Data Source</label>
          <button
            type="button"
            onClick={() => reload()}
            className="text-xs text-violet hover:underline"
          >
            Refresh
          </button>
        </div>
        <select
          className="w-full border border-ink/20 rounded-lg px-3 py-2 text-sm bg-white focus:outline-none focus:ring-2 focus:ring-violet"
          value={dataSourceId}
          onChange={e => setDataSourceId(e.target.value)}
          disabled={loadingSources}
        >
          <option value="">{loadingSources ? 'Loading...' : 'Select a data source'}</option>
          {usableSources.map(ds => (
            <option key={ds.id} value={ds.id}>{ds.name} ({ds.type})</option>
          ))}
        </select>
        {failedCount > 0 && (
          <p className="text-xs text-ink/40 mt-1">
            {failedCount} failed source{failedCount !== 1 ? 's' : ''} hidden —{' '}
            <a href="/settings" className="text-violet underline">manage in Settings</a>
          </p>
        )}
        <p className="text-xs text-ink/40 mt-1">
          Don't see the one you need?{' '}
          <a href="/settings" className="text-violet underline">add a new data source</a>
        </p>
      </div>

      {dataSourceId && (
        <div>
          <label className="block text-sm font-medium mb-1">Table</label>
          <select
            className="w-full border border-ink/20 rounded-lg px-3 py-2 text-sm bg-white focus:outline-none focus:ring-2 focus:ring-violet"
            value={tableName}
            onChange={e => setTableName(e.target.value)}
            disabled={loadingTables}
          >
            <option value="">{loadingTables ? 'Loading tables...' : 'Select a table'}</option>
            {tables.map(t => <option key={t} value={t}>{t}</option>)}
          </select>
        </div>
      )}

      {tableName && (
        <>
          <div className="grid sm:grid-cols-2 gap-4">
            <div>
              <label className="block text-sm font-medium mb-1">Primary Key Column</label>
              <select
                className="w-full border border-ink/20 rounded-lg px-3 py-2 text-sm bg-white focus:outline-none focus:ring-2 focus:ring-violet"
                value={primaryKeyColumn}
                onChange={e => setPrimaryKeyColumn(e.target.value)}
                disabled={loadingColumns}
              >
                <option value="">{loadingColumns ? 'Loading columns...' : 'Select column'}</option>
                {columns.map(c => (
                  <option key={c.name} value={c.name}>{c.name} ({c.dataType})</option>
                ))}
              </select>
              <p className="text-xs text-ink/40 mt-1">Must be an integer or UUID column.</p>
            </div>
            <div>
              <label className="block text-sm font-medium mb-1">Email Column</label>
              <select
                className="w-full border border-ink/20 rounded-lg px-3 py-2 text-sm bg-white focus:outline-none focus:ring-2 focus:ring-violet"
                value={emailColumn}
                onChange={e => setEmailColumn(e.target.value)}
                disabled={loadingColumns}
              >
                <option value="">{loadingColumns ? 'Loading columns...' : 'Select column'}</option>
                {columns.map(c => (
                  <option key={c.name} value={c.name}>{c.name} ({c.dataType})</option>
                ))}
              </select>
            </div>
          </div>

          <div className="grid sm:grid-cols-2 gap-4">
            <div>
              <label className="block text-sm font-medium mb-1">First Name Column <span className="text-ink/30">(optional)</span></label>
              <select
                className="w-full border border-ink/20 rounded-lg px-3 py-2 text-sm bg-white focus:outline-none focus:ring-2 focus:ring-violet"
                value={firstNameColumn}
                onChange={e => setFirstNameColumn(e.target.value)}
                disabled={loadingColumns}
              >
                <option value="">None</option>
                {columns.map(c => (
                  <option key={c.name} value={c.name}>{c.name} ({c.dataType})</option>
                ))}
              </select>
            </div>
            <div>
              <label className="block text-sm font-medium mb-1">Last Name Column <span className="text-ink/30">(optional)</span></label>
              <select
                className="w-full border border-ink/20 rounded-lg px-3 py-2 text-sm bg-white focus:outline-none focus:ring-2 focus:ring-violet"
                value={lastNameColumn}
                onChange={e => setLastNameColumn(e.target.value)}
                disabled={loadingColumns}
              >
                <option value="">None</option>
                {columns.map(c => (
                  <option key={c.name} value={c.name}>{c.name} ({c.dataType})</option>
                ))}
              </select>
            </div>
          </div>

          <div>
            <label className="block text-sm font-medium mb-1">Filter <span className="text-ink/30">(optional WHERE clause)</span></label>
            <input
              className="w-full border border-ink/20 rounded-lg px-3 py-2 text-sm font-mono focus:outline-none focus:ring-2 focus:ring-violet"
              placeholder="e.g. is_verified = true"
              value={whereClause}
              onChange={e => setWhereClause(e.target.value)}
            />
          </div>
        </>
      )}

      <button
        type="submit"
        disabled={!canSubmit || submitting}
        className="w-full bg-ink text-white text-sm font-medium py-2.5 rounded-lg hover:bg-violet transition disabled:opacity-50"
      >
        {submitting ? 'Starting import...' : 'Start Import'}
      </button>
    </form>
  )
}