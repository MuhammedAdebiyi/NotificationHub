import { useState, useEffect, useCallback } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import AppLayout from '@/app/layouts/AppLayout'
import { apiClient } from '@/shared/services/apiClient'

type NotificationStatus = 'Pending' | 'Processing' | 'Sent' | 'Failed' | 'Retrying' | 'DeadLetter'

interface NotificationLog {
  id: string
  provider: string
  response: string
  isSuccess: boolean
  createdAt: string
}

interface NotificationDetail {
  publicId: string
  recipientEmail: string
  type: string
  channel: string
  status: NotificationStatus
  payload: string
  retryCount: number
  createdAt: string
  lastProvider: string | null
  lastError: string | null
  logs: NotificationLog[]
}

const statusConfig: Record<NotificationStatus, { color: string; bg: string; label: string;  }> = {
  Pending:    { color: 'text-ink/60',  bg: 'bg-ink/10',     label: 'Pending',      },
  Processing: { color: 'text-violet',  bg: 'bg-violet/10',  label: 'Processing',  },
  Sent:       { color: 'text-teal',    bg: 'bg-teal/10',    label: 'Sent',          },
  Failed:     { color: 'text-coral',   bg: 'bg-coral/10',   label: 'Failed',       },
  Retrying:   { color: 'text-yellow',  bg: 'bg-yellow/20',  label: 'Retrying',     },
  DeadLetter: { color: 'text-coral',   bg: 'bg-coral/20',   label: 'Dead Letter',  },
}

export default function NotificationDetailPage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const [notification, setNotification] = useState<NotificationDetail | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [retrying, setRetrying] = useState(false)
  const [payloadExpanded, setPayloadExpanded] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const load = useCallback(async () => {
    if (!id) return
    try {
      const data = await apiClient.get<NotificationDetail>(`/api/v1/notifications/${id}`)
      setNotification(data)
    } catch {
      setError('Notification not found.')
    } finally {
      setIsLoading(false)
    }
  }, [id])

  useEffect(() => { load() }, [load])

  async function handleRetry() {
    if (!id) return
    setRetrying(true)
    try {
      await apiClient.post(`/api/v1/notifications/${id}/retry`, {})
      // Poll until status changes from Pending/Processing
      const poll = setInterval(async () => {
        const data = await apiClient.get<NotificationDetail>(`/api/v1/notifications/${id}`)
        setNotification(data)
        if (data.status !== 'Pending' && data.status !== 'Processing') {
          clearInterval(poll)
          setRetrying(false)
        }
      }, 2000)
      setTimeout(() => { clearInterval(poll); setRetrying(false) }, 30000)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Retry failed.')
      setRetrying(false)
    }
  }

  function formatPayload(raw: string) {
    try { return JSON.stringify(JSON.parse(raw), null, 2) }
    catch { return raw }
  }

  if (isLoading) return (
    <AppLayout>
      <div className="flex items-center justify-center h-64 text-ink/40 text-sm">Loading...</div>
    </AppLayout>
  )

  if (error || !notification) return (
    <AppLayout>
      <div className="text-center py-20">
        <p className="text-coral mb-4">{error ?? 'Notification not found.'}</p>
        <button onClick={() => navigate('/notifications')} className="text-sm text-violet hover:underline">
          ← Back to notifications
        </button>
      </div>
    </AppLayout>
  )

  const status = statusConfig[notification.status] ?? statusConfig.Pending
  const canRetry = notification.status === 'Failed' || notification.status === 'DeadLetter'

  return (
    <AppLayout>
      <div className="mb-6">
        <button
          onClick={() => navigate('/notifications')}
          className="text-sm text-ink/50 hover:text-ink transition mb-4 inline-flex items-center gap-1"
        >
          ← Back to notifications
        </button>

        {/* Hero card */}
        <div className={`${status.bg} border border-ink/10 rounded-2xl p-6 mb-6`}>
          <div className="flex items-start justify-between">
            <div>
              <div className="flex items-center gap-3 mb-2">
                
                <span className={`font-display font-bold text-2xl ${status.color}`}>
                  {status.label}
                </span>
              </div>
              <p className="text-ink/60 text-sm mb-1">
                To <strong className="text-ink">{notification.recipientEmail}</strong>
              </p>
              <p className="text-ink/40 text-xs font-mono">{notification.publicId}</p>
            </div>
            <div className="text-right">
              <p className="text-xs text-ink/40 mb-1">{notification.channel} · {notification.type}</p>
              <p className="text-xs text-ink/40">
                {new Date(notification.createdAt).toLocaleString()}
              </p>
              {canRetry && (
                <button
                  onClick={handleRetry}
                  disabled={retrying}
                  className="mt-3 px-4 py-2 bg-ink text-white text-xs font-semibold rounded-lg hover:bg-violet transition disabled:opacity-50"
                >
                  {retrying ? 'Retrying...' : 'Retry now'}
                </button>
              )}
            </div>
          </div>
        </div>

        {/* Metrics row */}
        <div className="grid grid-cols-3 gap-4 mb-6">
          <div className="bg-fog border border-ink/10 rounded-xl p-4 text-center">
            <p className="font-display font-bold text-2xl text-ink">{notification.retryCount}</p>
            <p className="text-xs text-ink/50 mt-1">Attempts</p>
          </div>
          <div className="bg-fog border border-ink/10 rounded-xl p-4 text-center">
            <p className="font-display font-bold text-lg text-ink truncate">
              {notification.lastProvider ?? '—'}
            </p>
            <p className="text-xs text-ink/50 mt-1">Last Provider</p>
          </div>
          <div className="bg-fog border border-ink/10 rounded-xl p-4 text-center">
            <p className="font-display font-bold text-lg text-ink">{notification.channel}</p>
            <p className="text-xs text-ink/50 mt-1">Channel</p>
          </div>
        </div>
      </div>

      <div className="grid lg:grid-cols-2 gap-6">
        {/* Delivery timeline */}
        <div className="bg-fog border border-ink/10 rounded-xl p-6">
          <h2 className="font-display font-bold text-lg mb-4">Delivery Timeline</h2>
          {notification.logs.length === 0 ? (
            <div className="text-center py-8 text-ink/30 text-sm">
              {notification.status === 'Pending'
                ? 'Waiting in queue...'
                : 'No delivery logs yet'}
            </div>
          ) : (
            <div className="relative">
              <div className="absolute left-4 top-0 bottom-0 w-px bg-ink/10" />
              <div className="space-y-4">
                {/* Created event */}
                <div className="flex gap-4 relative">
                  <div className="w-8 h-8 rounded-full bg-ink/10 flex items-center justify-center shrink-0 z-10 text-xs">
                    +
                  </div>
                  <div className="flex-1 pb-2">
                    <p className="text-sm font-medium">Created</p>
                    <p className="text-xs text-ink/40">
                      {new Date(notification.createdAt).toLocaleString()}
                    </p>
                  </div>
                </div>

                {/* Log events */}
                {notification.logs.map((log, i) => (
                  <div key={log.id} className="flex gap-4 relative">
                    <div className={`w-8 h-8 rounded-full flex items-center justify-center shrink-0 z-10 text-xs font-bold
                      ${log.isSuccess ? 'bg-teal/20 text-teal' : 'bg-coral/20 text-coral'}`}>
                      {i + 1}
                    </div>
                    <div className="flex-1 pb-2">
                      <div className="flex items-center gap-2 mb-0.5">
                        <p className="text-sm font-medium">
                          {log.isSuccess ? 'Delivered' : 'Attempt failed'}
                        </p>
                        <span className="text-xs text-ink/40">{log.provider}</span>
                      </div>
                      <p className="text-xs text-ink/50 font-mono leading-relaxed line-clamp-2">
                        {log.response}
                      </p>
                      <p className="text-xs text-ink/30 mt-1">
                        {new Date(log.createdAt).toLocaleString()}
                      </p>
                    </div>
                  </div>
                ))}

                {/* Final status */}
                <div className="flex gap-4 relative">
                  <div className={`w-8 h-8 rounded-full flex items-center justify-center shrink-0 z-10 text-xs
                    ${status.bg} ${status.color}`}>
                    
                  </div>
                  <div className="flex-1">
                    <p className={`text-sm font-medium ${status.color}`}>{status.label}</p>
                    <p className="text-xs text-ink/40">Current status</p>
                  </div>
                </div>
              </div>
            </div>
          )}
        </div>

        {/* Payload */}
        <div className="bg-fog border border-ink/10 rounded-xl p-6">
          <div className="flex items-center justify-between mb-4">
            <h2 className="font-display font-bold text-lg">Payload</h2>
            <button
              onClick={() => setPayloadExpanded(!payloadExpanded)}
              className="text-xs text-violet hover:underline"
            >
              {payloadExpanded ? 'Collapse' : 'Expand'}
            </button>
          </div>
          <pre className={`text-xs font-mono text-ink/70 bg-white border border-ink/10 rounded-lg p-4 overflow-x-auto transition-all
            ${payloadExpanded ? '' : 'max-h-48 overflow-y-hidden'}`}>
            {formatPayload(notification.payload)}
          </pre>
          {!payloadExpanded && (
            <button
              onClick={() => setPayloadExpanded(true)}
              className="text-xs text-violet mt-2 hover:underline"
            >
              Show full payload
            </button>
          )}
        </div>
      </div>
    </AppLayout>
  )
}