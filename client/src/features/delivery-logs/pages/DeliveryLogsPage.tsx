import { useState, useEffect } from 'react'
import AppLayout from '@/app/layouts/AppLayout'
import { apiClient } from '@/shared/api/apiClient'

interface LogEntry {
  id: string
  notificationId: string
  notificationPublicId: string
  recipientEmail: string
  status: string
  provider: string
  response: string
  isSuccess: boolean
  createdAt: string
}

export default function LogsPage() {
  const [logs, setLogs] = useState<LogEntry[]>([])
  const [total, setTotal] = useState(0)
  const [page, setPage] = useState(1)
  const [loading, setLoading] = useState(true)
  const [providerFilter, setProviderFilter] = useState('')
  const [statusFilter, setStatusFilter] = useState('')
  const pageSize = 25

  useEffect(() => { load() }, [page, providerFilter, statusFilter])

  async function load() {
    setLoading(true)
    try {
      const params = new URLSearchParams({ page: String(page), pageSize: String(pageSize) })
      if (providerFilter) params.set('provider', providerFilter)
      if (statusFilter) params.set('status', statusFilter)
      const res = await apiClient.get<{ items: LogEntry[]; totalCount: number }>(`/api/v1/notifications/logs?${params}`)
      setLogs(res.items)
      setTotal(res.totalCount)
    } catch { /* ignore */ }
    setLoading(false)
  }

  const providers = ['SendByte', 'Resend', 'SendGrid', 'Brevo', 'Smtp']
  const statuses = ['Sent', 'Failed', 'Pending', 'Processing', 'Retrying', 'DeadLetter']

  return (
    <AppLayout>
      <div className="mb-8">
        <p className="hand text-xl text-violet mb-1">delivery trail —</p>
        <h1 className="font-display font-bold text-3xl">Notification Logs</h1>
      </div>

      <div className="bg-white border border-ink/10 rounded-xl p-4 mb-6 flex flex-wrap items-end gap-4">
        <div>
          <label className="block text-xs font-medium text-ink/50 mb-1">Provider</label>
          <select
            value={providerFilter}
            onChange={e => { setProviderFilter(e.target.value); setPage(1) }}
            className="border border-ink/20 rounded-lg px-3 py-1.5 text-sm bg-white focus:outline-none focus:ring-2 focus:ring-violet"
          >
            <option value="">All Providers</option>
            {providers.map(p => <option key={p} value={p}>{p}</option>)}
          </select>
        </div>
        <div>
          <label className="block text-xs font-medium text-ink/50 mb-1">Status</label>
          <select
            value={statusFilter}
            onChange={e => { setStatusFilter(e.target.value); setPage(1) }}
            className="border border-ink/20 rounded-lg px-3 py-1.5 text-sm bg-white focus:outline-none focus:ring-2 focus:ring-violet"
          >
            <option value="">All Statuses</option>
            {statuses.map(s => <option key={s} value={s}>{s}</option>)}
          </select>
        </div>
        <span className="text-sm text-ink/50">{total} logs</span>
      </div>

      {loading ? (
        <div className="text-center py-12 text-ink/40 text-sm">Loading...</div>
      ) : logs.length === 0 ? (
        <div className="bg-white border border-ink/10 rounded-xl p-12 text-center">
          <p className="font-display font-bold text-lg mb-2">No logs yet</p>
          <p className="text-sm text-ink/50">Send a notification to see delivery logs here.</p>
        </div>
      ) : (
        <div className="bg-white border border-ink/10 rounded-xl overflow-hidden">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-ink/10 text-left">
                <th className="px-4 py-3 font-medium text-ink/50">Status</th>
                <th className="px-4 py-3 font-medium text-ink/50">Provider</th>
                <th className="px-4 py-3 font-medium text-ink/50">Recipient</th>
                <th className="px-4 py-3 font-medium text-ink/50">Response</th>
                <th className="px-4 py-3 font-medium text-ink/50">Time</th>
              </tr>
            </thead>
            <tbody>
              {logs.map(log => (
                <tr key={log.id} className="border-b border-ink/5 hover:bg-fog/50 transition">
                  <td className="px-4 py-3">
                    <span className={`inline-flex items-center gap-1.5 text-xs font-medium px-2 py-0.5 rounded-full ${
                      log.isSuccess ? 'bg-teal/10 text-teal' : 'bg-coral/10 text-coral'
                    }`}>
                      <span className={`w-1.5 h-1.5 rounded-full ${log.isSuccess ? 'bg-teal' : 'bg-coral'}`} />
                      {log.status}
                    </span>
                  </td>
                  <td className="px-4 py-3 font-medium">{log.provider}</td>
                  <td className="px-4 py-3 text-ink/60">{log.recipientEmail}</td>
                  <td className="px-4 py-3 text-ink/40 font-mono text-xs max-w-[200px] truncate">{log.response}</td>
                  <td className="px-4 py-3 text-ink/40 text-xs">{new Date(log.createdAt).toLocaleString()}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {total > pageSize && (
        <div className="flex justify-center gap-2 mt-6">
          <button
            onClick={() => setPage(p => Math.max(1, p - 1))}
            disabled={page === 1}
            className="px-3 py-1.5 text-sm border border-ink/20 rounded-lg hover:bg-fog transition disabled:opacity-50"
          >
            Previous
          </button>
          <span className="px-3 py-1.5 text-sm text-ink/50">Page {page} of {Math.ceil(total / pageSize)}</span>
          <button
            onClick={() => setPage(p => p + 1)}
            disabled={page >= Math.ceil(total / pageSize)}
            className="px-3 py-1.5 text-sm border border-ink/20 rounded-lg hover:bg-fog transition disabled:opacity-50"
          >
            Next
          </button>
        </div>
      )}
    </AppLayout>
  )
}
