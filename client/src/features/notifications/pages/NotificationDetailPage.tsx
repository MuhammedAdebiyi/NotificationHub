import { useState, useEffect, useCallback } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import AppLayout from '@/app/layouts/AppLayout'
import NotificationStatusBadge from '../components/NotificationStatusBadge'
import { notificationApi } from '../api/notificationApi'
import { apiClient } from '@/shared/services/apiClient'
import type { NotificationDetail } from '../types/notification.types'

interface DeliveryLog {
  id: string
  provider: string
  response: string
  createdAt: string
}

export default function NotificationDetailPage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const [notification, setNotification] = useState<NotificationDetail | null>(null)
  const [logs, setLogs] = useState<DeliveryLog[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [retrying, setRetrying] = useState(false)
  const [retrySuccess, setRetrySuccess] = useState(false)

  const load = useCallback(async () => {
    if (!id) return
    try {
      const [notif, fetchedLogs] = await Promise.all([
        notificationApi.getById(id),
        apiClient.get<DeliveryLog[]>(`/api/v1/notifications/${id}/logs`),
      ])
      setNotification(notif)
      setLogs(fetchedLogs)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load notification.')
    } finally {
      setIsLoading(false)
    }
  }, [id])

  useEffect(() => { load() }, [load])

  async function handleRetry() {
    if (!id) return
    setRetrying(true)
    setRetrySuccess(false)
    try {
      await notificationApi.retry(id)
      setRetrySuccess(true)
      // Reload to show updated status
      await load()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Retry failed.')
    } finally {
      setRetrying(false)
    }
  }

  const canRetry = notification?.status === 'DeadLetter' || notification?.status === 'Failed'

  return (
    <AppLayout>
      <div className="mb-6">
        <button
          onClick={() => navigate('/notifications')}
          className="text-sm text-ink/50 hover:text-ink transition mb-4 inline-flex items-center gap-1"
        >
          ← Back to notifications
        </button>
        <div className="flex items-start justify-between">
          <div>
            <p className="hand text-xl text-violet mb-1">notification —</p>
            <h1 className="font-display font-bold text-3xl">Notification Detail</h1>
          </div>
          {canRetry && (
            <div className="flex flex-col items-end gap-2">
              <button
                onClick={handleRetry}
                disabled={retrying}
                className="px-4 py-2 bg-violet text-white text-sm font-medium rounded-lg hover:bg-violet/80 transition disabled:opacity-50"
              >
                {retrying ? 'Re-queuing...' : '↺ Retry now'}
              </button>
              {retrySuccess && (
                <p className="text-xs text-teal">✓ Re-queued successfully — status will update shortly</p>
              )}
            </div>
          )}
        </div>
      </div>

      {isLoading && <div className="text-ink/40 text-sm">Loading...</div>}
      {error && <div className="bg-coral/10 text-coral text-sm px-4 py-3 rounded-xl mb-4">{error}</div>}

      {notification && (
        <div className="grid gap-5">
          {/* Main details */}
          <div className="bg-fog border border-ink/10 rounded-xl p-6">
            <div className="grid sm:grid-cols-2 gap-4">
              <Detail label="Public ID" value={notification.publicId} mono />
              <Detail label="Recipient" value={notification.recipientEmail ?? '—'} />
              <Detail label="Type" value={notification.type} />
              <Detail label="Channel" value={notification.channel} />
              <Detail
                label="Status"
                value={
                  <div className="flex items-center gap-2">
                    <NotificationStatusBadge status={notification.status} />
                    {canRetry && (
                      <span className="text-xs text-ink/40">
                        {notification.status === 'DeadLetter'
                          ? 'Exceeded max retries'
                          : 'Delivery failed'}
                      </span>
                    )}
                  </div>
                }
              />
              <Detail label="Retry Count" value={String(notification.retryCount)} />
              <Detail label="Created" value={new Date(notification.createdAt).toLocaleString()} />
            </div>
          </div>

          {/* DLQ warning */}
          {notification.status === 'DeadLetter' && (
            <div className="bg-coral/10 border border-coral/20 rounded-xl px-5 py-4">
              <p className="font-medium text-coral text-sm mb-1">Dead Letter Queue</p>
              <p className="text-xs text-ink/60">
                This notification failed after {notification.retryCount} attempts and was moved to the DLQ.
                Check the delivery logs below to see the exact error. You can retry it manually — it will
                start fresh with 0 retry count.
              </p>
            </div>
          )}

          {/* Payload */}
          <div className="bg-fog border border-ink/10 rounded-xl p-6">
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

          {/* Delivery logs */}
          <div className="bg-fog border border-ink/10 rounded-xl p-6">
            <h2 className="font-display font-bold text-lg mb-3">
              Delivery Logs
              <span className="ml-2 text-xs font-normal text-ink/40">
                {logs.length} attempt{logs.length !== 1 ? 's' : ''}
              </span>
            </h2>
            {logs.length === 0 ? (
              <p className="text-sm text-ink/40">No delivery attempts yet.</p>
            ) : (
              <div className="space-y-3">
                {logs.map((log, i) => {
                  let isError = false
                  let parsed: Record<string, unknown> | null = null
                  try {
                    parsed = JSON.parse(log.response)
                    isError = !!(parsed?.error)
                  } catch {
                    isError = log.response.toLowerCase().includes('error') ||
                               log.response.toLowerCase().includes('fail')
                  }

                  return (
                    <div
                      key={log.id}
                      className={`border rounded-xl p-4 text-sm ${
                        isError
                          ? 'bg-coral/5 border-coral/20'
                          : 'bg-white border-ink/10'
                      }`}
                    >
                      <div className="flex items-center justify-between mb-2">
                        <div className="flex items-center gap-2">
                          <span className="font-medium">{log.provider}</span>
                          <span className={`text-xs px-2 py-0.5 rounded-full font-medium ${
                            isError
                              ? 'bg-coral/10 text-coral'
                              : 'bg-teal/10 text-teal'
                          }`}>
                            {isError ? 'Failed' : 'Success'}
                          </span>
                          <span className="text-ink/30 text-xs">Attempt {i + 1}</span>
                        </div>
                        <span className="text-ink/40 text-xs">
                          {new Date(log.createdAt).toLocaleString()}
                        </span>
                      </div>
                      <pre className="text-xs font-mono text-ink/60 bg-white/60 rounded-lg p-3 overflow-x-auto whitespace-pre-wrap">
                        {parsed
                          ? JSON.stringify(parsed, null, 2)
                          : log.response}
                      </pre>
                    </div>
                  )
                })}
              </div>
            )}
          </div>
        </div>
      )}
    </AppLayout>
  )
}

function Detail({ label, value, mono }: { label: string; value: React.ReactNode; mono?: boolean }) {
  return (
    <div>
      <p className="text-xs text-ink/40 uppercase tracking-wide mb-1">{label}</p>
      <p className={`text-sm font-medium break-all ${mono ? 'font-mono text-ink/60' : ''}`}>{value}</p>
    </div>
  )
}