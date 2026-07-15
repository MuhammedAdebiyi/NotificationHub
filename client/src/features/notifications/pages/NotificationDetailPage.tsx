import { useState, useEffect, useCallback, useMemo } from 'react'
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

interface ProviderInfo {
  name: string
  status: 'healthy' | 'warning' | 'critical'
  avgLatencyMs: number
  successRateToday: number
}

interface CampaignRef {
  id: string
  title: string
  status: string
  recipientCount: number
}

interface TemplateRef {
  name: string
  version: number
  channel: string
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

  // Optional enrichment fields. Backend does not send these yet for every
  // notification — the UI degrades gracefully (hides the row/section) when
  // a field is missing rather than showing "undefined" or a fake zero.
  correlationId?: string
  organizationName?: string
  apiKeyName?: string
  workerId?: string
  acceptedAt?: string
  completedAt?: string
  provider?: ProviderInfo
  campaign?: CampaignRef
  template?: TemplateRef
}

const statusConfig: Record<
  NotificationStatus,
  { color: string; bg: string; ring: string; label: string; summary: string }
> = {
  Pending:    { color: 'text-ink/60', bg: 'bg-ink/10',    ring: 'ring-ink/10',    label: 'Pending',     summary: 'Waiting in queue' },
  Processing: { color: 'text-violet', bg: 'bg-violet/10', ring: 'ring-violet/20', label: 'Processing',  summary: 'Being sent right now' },
  Sent:       { color: 'text-teal',   bg: 'bg-teal/10',   ring: 'ring-teal/20',   label: 'Delivered',   summary: 'Delivered successfully' },
  Failed:     { color: 'text-coral',  bg: 'bg-coral/10',  ring: 'ring-coral/20',  label: 'Failed',      summary: 'Delivery failed' },
  Retrying:   { color: 'text-yellow', bg: 'bg-yellow/20', ring: 'ring-yellow/30', label: 'Retrying',    summary: 'Retry in progress' },
  DeadLetter: { color: 'text-coral',  bg: 'bg-coral/20',  ring: 'ring-coral/30',  label: 'Dead Letter', summary: 'Exhausted all retries' },
}

function formatMs(ms: number | null): string {
  if (ms === null || Number.isNaN(ms)) return '—'
  if (ms < 1000) return `${Math.round(ms)} ms`
  return `${(ms / 1000).toFixed(1)} s`
}

function formatBytes(str: string): string {
  const bytes = new Blob([str]).size
  if (bytes < 1024) return `${bytes} B`
  return `${(bytes / 1024).toFixed(1)} KB`
}

function diffMs(a?: string | null, b?: string | null): number | null {
  if (!a || !b) return null
  const d = new Date(b).getTime() - new Date(a).getTime()
  return Number.isNaN(d) ? null : d
}

/** Attempts a human-readable read of an email-shaped payload. Falls back to null if the
 *  payload doesn't look like an email (no subject/body-ish fields), so callers can fall
 *  back to raw JSON instead of showing a misleading empty card. */
function parseEmailPreview(raw: string): { subject?: string; body?: string; isHtml: boolean } | null {
  try {
    const obj = JSON.parse(raw)
    const lower: Record<string, unknown> = {}
    for (const k of Object.keys(obj)) lower[k.toLowerCase()] = obj[k]
    const subject = (lower['subject'] as string) || (lower['title'] as string)
    const body =
      (lower['body'] as string) ||
      (lower['html'] as string) ||
      (lower['text'] as string) ||
      (lower['message'] as string)
    if (!subject && !body) return null
    const isHtml = typeof body === 'string' && /<[a-z][\s\S]*>/i.test(body)
    return { subject, body, isHtml }
  } catch {
    return null
  }
}

export default function NotificationDetailPage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const [notification, setNotification] = useState<NotificationDetail | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [retrying, setRetrying] = useState(false)
  const [payloadView, setPayloadView] = useState<'preview' | 'raw'>('preview')
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

  const emailPreview = useMemo(
    () => (notification ? parseEmailPreview(notification.payload) : null),
    [notification]
  )

  const lastLogAt = notification?.logs.length
    ? notification.logs[notification.logs.length - 1].createdAt
    : null

  const deliveryTimeMs = notification
    ? diffMs(notification.createdAt, notification.completedAt ?? lastLogAt)
    : null
  const queueWaitMs = notification
    ? diffMs(notification.createdAt, notification.acceptedAt)
    : null

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
  const isTerminal = notification.status === 'Sent' || notification.status === 'Failed' || notification.status === 'DeadLetter'

  return (
    <AppLayout>
      <div className="mb-6">
        <button
          onClick={() => navigate('/notifications')}
          className="text-sm text-ink/50 hover:text-ink transition mb-4 inline-flex items-center gap-1"
        >
          ← Back to notifications
        </button>

        {/* Hero: the whole story in one glance */}
        <div className={`${status.bg} border border-ink/10 rounded-2xl p-6 mb-4`}>
          <div className="flex items-start justify-between gap-4 flex-wrap">
            <div>
              <div className="flex items-center gap-2 mb-2">
                <span className={`w-2.5 h-2.5 rounded-full ${status.color.replace('text-', 'bg-')}`} />
                <span className={`font-display font-bold text-2xl ${status.color}`}>
                  {status.label}
                </span>
              </div>
              <p className="text-ink/60 text-sm mb-3">{status.summary}</p>

              {/* quick-glance badges */}
              <div className="flex flex-wrap gap-2">
                {isTerminal && deliveryTimeMs !== null && (
                  <span className="px-2.5 py-1 rounded-full bg-white/70 text-xs font-medium text-ink/70">
                    {formatMs(deliveryTimeMs)} to deliver
                  </span>
                )}
                {notification.lastProvider && (
                  <span className="px-2.5 py-1 rounded-full bg-white/70 text-xs font-medium text-ink/70">
                    via {notification.lastProvider}
                  </span>
                )}
                <span className="px-2.5 py-1 rounded-full bg-white/70 text-xs font-medium text-ink/70">
                  Attempt {Math.max(notification.retryCount, notification.logs.length, 1)} of 5
                </span>
              </div>

              <p className="text-ink/60 text-sm mt-3">
                To <strong className="text-ink">{notification.recipientEmail}</strong>
              </p>
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

        {/* Delivery metrics — only rendered when the backend actually sent the field */}
        <div className="grid grid-cols-2 sm:grid-cols-5 gap-3 mb-6">
          <MetricCard label="Delivery Time" value={isTerminal ? formatMs(deliveryTimeMs) : '—'} />
          <MetricCard label="Queue Wait" value={formatMs(queueWaitMs)} />
          <MetricCard label="Attempts" value={String(Math.max(notification.retryCount, notification.logs.length, 1))} />
          <MetricCard label="Worker" value={notification.workerId ?? '—'} mono />
          <MetricCard label="Channel" value={notification.channel} />
        </div>
      </div>

      <div className="grid lg:grid-cols-2 gap-6">
        {/* Delivery timeline */}
        <div className="bg-fog border border-ink/10 rounded-xl p-6">
          <h2 className="font-display font-bold text-lg mb-4">Delivery Timeline</h2>
          <div className="relative">
            <div className="absolute left-4 top-0 bottom-0 w-px bg-ink/10" />
            <div className="space-y-4">
              {/* Created */}
              <TimelineNode
                label="Created"
                timestamp={notification.createdAt}
                tone="neutral"
              />

              {/* Accepted by worker, if known */}
              {notification.acceptedAt && (
                <TimelineNode
                  label="Worker picked up"
                  timestamp={notification.acceptedAt}
                  tone="neutral"
                  detail={notification.workerId ? `worker ${notification.workerId}` : undefined}
                />
              )}

              {notification.logs.length === 0 && notification.status === 'Pending' && (
                <div className="ml-12 text-sm text-ink/30">Waiting in queue...</div>
              )}

              {/* One node per delivery attempt */}
              {notification.logs.map((log, i) => (
                <TimelineNode
                  key={log.id}
                  label={log.isSuccess ? 'Provider accepted' : `Attempt ${i + 1} failed`}
                  timestamp={log.createdAt}
                  tone={log.isSuccess ? 'success' : 'error'}
                  detail={`${log.provider} · ${log.response}`}
                />
              ))}

              {/* Final status */}
              <TimelineNode
                label={status.label}
                timestamp={notification.completedAt}
                tone={notification.status === 'Sent' ? 'success' : notification.status === 'Failed' || notification.status === 'DeadLetter' ? 'error' : 'active'}
                isFinal
              />
            </div>
          </div>
        </div>

        <div className="flex flex-col gap-6">
          {/* Provider */}
          {notification.provider && (
            <div className="bg-fog border border-ink/10 rounded-xl p-6">
              <h2 className="font-display font-bold text-lg mb-4">Provider</h2>
              <div className="flex items-center justify-between mb-3">
                <span className="font-medium text-ink">{notification.provider.name}</span>
                <span className={`text-xs px-2 py-0.5 rounded-full ${
                  notification.provider.status === 'healthy' ? 'bg-teal/10 text-teal'
                  : notification.provider.status === 'warning' ? 'bg-yellow/20 text-yellow'
                  : 'bg-coral/10 text-coral'
                }`}>
                  {notification.provider.status}
                </span>
              </div>
              <div className="grid grid-cols-2 gap-3 text-sm">
                <div>
                  <p className="text-ink/40 text-xs">Avg latency</p>
                  <p className="text-ink font-medium">{formatMs(notification.provider.avgLatencyMs)}</p>
                </div>
                <div>
                  <p className="text-ink/40 text-xs">Today's success</p>
                  <p className="text-ink font-medium">{notification.provider.successRateToday.toFixed(1)}%</p>
                </div>
              </div>
            </div>
          )}

          {/* Campaign context */}
          {notification.campaign && (
            <div className="bg-fog border border-ink/10 rounded-xl p-6">
              <h2 className="font-display font-bold text-lg mb-3">Campaign</h2>
              <p className="font-medium text-ink mb-1">{notification.campaign.title}</p>
              <p className="text-sm text-ink/50 mb-3">
                {notification.campaign.status} · {notification.campaign.recipientCount.toLocaleString()} recipients
              </p>
              <button
                onClick={() => navigate(`/campaigns/${notification.campaign!.id}`)}
                className="text-sm text-violet hover:underline"
              >
                View campaign →
              </button>
            </div>
          )}

          {/* Template context */}
          {notification.template && (
            <div className="bg-fog border border-ink/10 rounded-xl p-6">
              <h2 className="font-display font-bold text-lg mb-3">Template</h2>
              <p className="font-medium text-ink mb-1">{notification.template.name}</p>
              <p className="text-sm text-ink/50">
                Version {notification.template.version} · {notification.template.channel}
              </p>
            </div>
          )}

          {/* Metadata */}
          <div className="bg-fog border border-ink/10 rounded-xl p-6">
            <h2 className="font-display font-bold text-lg mb-4">Metadata</h2>
            <dl className="text-sm space-y-2">
              <MetaRow label="Notification ID" value={notification.publicId} mono />
              {notification.correlationId && (
                <MetaRow label="Correlation ID" value={notification.correlationId} mono />
              )}
              {notification.organizationName && (
                <MetaRow label="Organization" value={notification.organizationName} />
              )}
              {notification.apiKeyName && (
                <MetaRow label="API Key" value={notification.apiKeyName} />
              )}
              <MetaRow label="Created" value={new Date(notification.createdAt).toLocaleString()} />
              {notification.completedAt && (
                <MetaRow label="Updated" value={new Date(notification.completedAt).toLocaleString()} />
              )}
            </dl>
          </div>
        </div>
      </div>

      {/* Payload */}
      <div className="bg-fog border border-ink/10 rounded-xl p-6 mt-6">
        <div className="flex items-center justify-between mb-4 flex-wrap gap-2">
          <div className="flex items-center gap-2">
            <h2 className="font-display font-bold text-lg">Payload</h2>
            <span className="text-xs text-ink/40">{formatBytes(notification.payload)} · JSON</span>
          </div>
          <div className="flex items-center gap-3">
            {emailPreview && (
              <div className="flex bg-white border border-ink/10 rounded-lg p-0.5 text-xs">
                <button
                  onClick={() => setPayloadView('preview')}
                  className={`px-2.5 py-1 rounded-md transition ${payloadView === 'preview' ? 'bg-ink text-white' : 'text-ink/50'}`}
                >
                  Preview
                </button>
                <button
                  onClick={() => setPayloadView('raw')}
                  className={`px-2.5 py-1 rounded-md transition ${payloadView === 'raw' ? 'bg-ink text-white' : 'text-ink/50'}`}
                >
                  Raw JSON
                </button>
              </div>
            )}
            {(!emailPreview || payloadView === 'raw') && (
              <button
                onClick={() => setPayloadExpanded(!payloadExpanded)}
                className="text-xs text-violet hover:underline"
              >
                {payloadExpanded ? 'Collapse' : 'Expand'}
              </button>
            )}
          </div>
        </div>

        {emailPreview && payloadView === 'preview' ? (
          <div className="bg-white border border-ink/10 rounded-lg p-5">
            {emailPreview.subject && (
              <>
                <p className="text-xs text-ink/40 mb-1">Subject</p>
                <p className="font-medium text-ink mb-4">{emailPreview.subject}</p>
              </>
            )}
            <p className="text-xs text-ink/40 mb-1">To</p>
            <p className="text-sm text-ink mb-4">{notification.recipientEmail}</p>
            {emailPreview.body && (
              <>
                <p className="text-xs text-ink/40 mb-1">Body</p>
                <div className="border-t border-ink/10 pt-3">
                  {emailPreview.isHtml ? (
                    
                    <iframe
                      title="Email preview"
                      srcDoc={emailPreview.body}
                      sandbox=""
                      className="w-full h-[420px] rounded-lg border border-ink/10 bg-white"
                    />
                  ) : (
                    <div className="text-sm text-ink/80 whitespace-pre-wrap leading-relaxed">
                      {emailPreview.body}
                    </div>
                  )}
                </div>
              </>
            )}
          </div>
        ) : (
          <>
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
          </>
        )}
      </div>
    </AppLayout>
  )
}

function MetricCard({ label, value, mono }: { label: string; value: string; mono?: boolean }) {
  return (
    <div className="bg-fog border border-ink/10 rounded-xl p-4 text-center">
      <p className={`font-display font-bold text-lg text-ink truncate ${mono ? 'font-mono text-sm' : ''}`}>
        {value}
      </p>
      <p className="text-xs text-ink/50 mt-1">{label}</p>
    </div>
  )
}

function MetaRow({ label, value, mono }: { label: string; value: string; mono?: boolean }) {
  return (
    <div className="flex items-center justify-between gap-4">
      <dt className="text-ink/40">{label}</dt>
      <dd className={`text-ink text-right truncate max-w-[60%] ${mono ? 'font-mono text-xs' : ''}`}>{value}</dd>
    </div>
  )
}

function TimelineNode({
  label,
  timestamp,
  detail,
  tone,
  isFinal,
}: {
  label: string
  timestamp?: string | null
  detail?: string
  tone: 'neutral' | 'success' | 'error' | 'active'
  isFinal?: boolean
}) {
  const dotClass =
    tone === 'success' ? 'bg-teal/20 text-teal'
    : tone === 'error' ? 'bg-coral/20 text-coral'
    : tone === 'active' ? 'bg-violet/20 text-violet'
    : 'bg-ink/10 text-ink/50'

  const labelClass =
    tone === 'success' ? 'text-teal'
    : tone === 'error' ? 'text-coral'
    : tone === 'active' ? 'text-violet'
    : 'text-ink'

  return (
    <div className="flex gap-4 relative">
      <div className={`w-8 h-8 rounded-full flex items-center justify-center shrink-0 z-10 text-xs font-bold ${dotClass}`}>
        {tone === 'success' ? '✓' : tone === 'error' ? '✕' : '●'}
      </div>
      <div className={`flex-1 ${isFinal ? '' : 'pb-2'}`}>
        <p className={`text-sm font-medium ${labelClass}`}>{label}</p>
        {detail && <p className="text-xs text-ink/50 font-mono leading-relaxed line-clamp-2">{detail}</p>}
        <p className="text-xs text-ink/30 mt-0.5">
          {timestamp ? new Date(timestamp).toLocaleString() : isFinal ? 'Current status' : ''}
        </p>
      </div>
    </div>
  )
}