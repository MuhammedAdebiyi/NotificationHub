import AppLayout from '@/app/layouts/AppLayout'
import NotificationTable from '../components/NotificationTable'
import { useNotifications } from '../hooks/useNotifications'

export default function NotificationsPage() {
  const {
    notifications,
    totalCount,
    isLoading,
    error,
    page,
    setPage,
    pageSize,
    setPageSize,
    dateFrom,
    setDateFrom,
    dateTo,
    setDateTo,
    status,
    setStatus,
  } = useNotifications()

  const totalPages = Math.ceil(totalCount / pageSize)

  return (
    <AppLayout>
      <div className="mb-8 flex items-center justify-between">
        <div>
          <p className="hand text-xl text-violet mb-1">all activity —</p>
          <h1 className="font-display font-bold text-3xl">Notifications</h1>
        </div>
        <span className="text-sm text-ink/50">{totalCount} total</span>
      </div>

      {/* Filters */}
      <div className="bg-white border border-ink/10 rounded-xl p-4 mb-6 flex flex-wrap items-end gap-4">
        <div>
          <label className="block text-xs font-medium text-ink/50 mb-1">From</label>
          <input
            type="date"
            value={dateFrom}
            onChange={e => { setDateFrom(e.target.value); setPage(1) }}
            className="border border-ink/20 rounded-lg px-3 py-1.5 text-sm bg-white focus:outline-none focus:ring-2 focus:ring-violet"
          />
        </div>
        <div>
          <label className="block text-xs font-medium text-ink/50 mb-1">To</label>
          <input
            type="date"
            value={dateTo}
            onChange={e => { setDateTo(e.target.value); setPage(1) }}
            className="border border-ink/20 rounded-lg px-3 py-1.5 text-sm bg-white focus:outline-none focus:ring-2 focus:ring-violet"
          />
        </div>
        <div>
          <label className="block text-xs font-medium text-ink/50 mb-1">Per page</label>
          <select
            value={pageSize}
            onChange={e => { setPageSize(Number(e.target.value)); setPage(1) }}
            className="border border-ink/20 rounded-lg px-3 py-1.5 text-sm bg-white focus:outline-none focus:ring-2 focus:ring-violet"
          >
            <option value={50}>50</option>
            <option value={100}>100</option>
          </select>
        </div>
        <div>
          <label className="block text-xs font-medium text-ink/50 mb-1">Status</label>
          <select
            value={status}
            onChange={e => { setStatus(e.target.value); setPage(1) }}
            className="border border-ink/20 rounded-lg px-3 py-1.5 text-sm bg-white focus:outline-none focus:ring-2 focus:ring-violet"
          >
            <option value="">All</option>
            <option value="Pending">Pending</option>
            <option value="Processing">Processing</option>
            <option value="Sent">Sent</option>
            <option value="Failed">Failed</option>
            <option value="Retrying">Retrying</option>
            <option value="DeadLetter">Dead Letter</option>
          </select>
        </div>
        {(dateFrom || dateTo || status) && (
          <button
            onClick={() => { setDateFrom(''); setDateTo(''); setStatus(''); setPage(1) }}
            className="text-xs px-3 py-1.5 border border-ink/20 rounded-lg hover:bg-fog transition"
          >
            Clear filters
          </button>
        )}
      </div>

      <div className="bg-fog border border-ink/10 rounded-lg p-6">
        {isLoading && (
          <div className="text-center py-16 text-ink/40 text-sm">Loading...</div>
        )}
        {error && (
          <div className="text-center py-16 text-coral text-sm">{error}</div>
        )}
        {!isLoading && !error && (
          <NotificationTable notifications={notifications} />
        )}

        {/* Pagination */}
        {totalPages > 1 && (
          <div className="flex items-center justify-between mt-6 pt-4 border-t border-ink/10">
            <p className="text-xs text-ink/40">
              Page {page} of {totalPages}
            </p>
            <div className="flex gap-2">
              <button
                onClick={() => setPage(Math.max(1, page - 1))}
                disabled={page <= 1}
                className="px-3 py-1.5 text-xs border border-ink/20 rounded-lg hover:bg-fog transition disabled:opacity-40"
              >
                Previous
              </button>
              <button
                onClick={() => setPage(Math.min(totalPages, page + 1))}
                disabled={page >= totalPages}
                className="px-3 py-1.5 text-xs border border-ink/20 rounded-lg hover:bg-fog transition disabled:opacity-40"
              >
                Next
              </button>
            </div>
          </div>
        )}
      </div>
    </AppLayout>
  )
}
