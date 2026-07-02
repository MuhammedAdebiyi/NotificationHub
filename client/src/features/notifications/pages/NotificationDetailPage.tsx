import { useParams, useNavigate } from 'react-router-dom'
import AppLayout from '@/app/layouts/AppLayout'
import NotificationStatusBadge from '../components/NotificationStatusBadge'
import { useNotificationDetail } from '../hooks/useNotificationDetail'

export default function NotificationDetailPage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const { notification, isLoading, error } = useNotificationDetail(id ?? '')

  return (
    <AppLayout>
      <div className="mb-6">
        <button
          onClick={() => navigate('/notifications')}
          className="text-sm text-ink/50 hover:text-ink transition mb-4 inline-flex items-center gap-1"
        >
          ← Back to notifications
        </button>
        <h1 className="font-display font-bold text-3xl">Notification Detail</h1>
      </div>

      {isLoading && <div className="text-ink/40 text-sm">Loading...</div>}
      {error && <div className="text-coral text-sm">{error}</div>}

      {notification && (
        <div className="grid gap-5">
          <div className="bg-fog border border-ink/10 rounded-lg p-6">
            <div className="grid sm:grid-cols-2 gap-4">
              <Detail label="Public ID" value={notification.publicId} mono />
              <Detail label="Type" value={notification.type} />
              <Detail label="Channel" value={notification.channel} />
              <Detail label="Status" value={<NotificationStatusBadge status={notification.status} />} />
              <Detail label="Retry Count" value={String(notification.retryCount)} />
              <Detail label="Created" value={new Date(notification.createdAt).toLocaleString()} />
            </div>
          </div>

          <div className="bg-fog border border-ink/10 rounded-lg p-6">
            <h2 className="font-display font-bold text-lg mb-3">Payload</h2>
            <pre className="text-xs font-mono text-ink/70 bg-white border border-ink/10 rounded-lg p-4 overflow-x-auto whitespace-pre-wrap">
            {(() => {
            try {
              return JSON.stringify(JSON.parse(notification.payload || '{}'), null, 2)
            } catch {
              return notification.payload
            }
          })()}
            </pre>
          </div>

          {notification.logs && notification.logs.length > 0 && (
            <div className="bg-fog border border-ink/10 rounded-lg p-6">
              <h2 className="font-display font-bold text-lg mb-3">Delivery Logs</h2>
              <div className="space-y-3">
                {notification.logs.map((log) => (
                  <div key={log.id} className="bg-white border border-ink/10 rounded-lg p-4 text-sm">
                    <div className="flex items-center justify-between mb-1">
                      <span className="font-medium">{log.provider}</span>
                      <span className="text-ink/40 text-xs">
                        {new Date(log.createdAt).toLocaleString()}
                      </span>
                    </div>
                    <p className="text-ink/60 font-mono text-xs">{log.response}</p>
                  </div>
                ))}
              </div>
            </div>
          )}
        </div>
      )}
    </AppLayout>
  )
}

function Detail({ label, value, mono }: { label: string; value: React.ReactNode; mono?: boolean }) {
  return (
    <div>
      <p className="text-xs text-ink/40 uppercase tracking-wide mb-1">{label}</p>
      <p className={`text-sm font-medium ${mono ? 'font-mono text-ink/60' : ''}`}>{value}</p>
    </div>
  )
}