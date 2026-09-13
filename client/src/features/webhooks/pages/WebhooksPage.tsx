import { useState, useEffect } from 'react'
import AppLayout from '@/app/layouts/AppLayout'
import { apiClient } from '@/shared/api/apiClient'

interface Webhook {
  id: string
  url: string
  events: string[]
  isActive: boolean
  lastTriggeredAt: string | null
  lastError: string | null
  createdAt: string
}

interface WebhookDelivery {
  id: string
  event: string
  statusCode: number
  isSuccess: boolean
  durationMs: number
  response: string | null
  createdAt: string
}

export default function WebhooksPage() {
  const [webhooks, setWebhooks] = useState<Webhook[]>([])
  const [loading, setLoading] = useState(true)
  const [showCreate, setShowCreate] = useState(false)
  const [newUrl, setNewUrl] = useState('')
  const [newEvents, setNewEvents] = useState('notification.sent,notification.failed')
  const [newSecret, setNewSecret] = useState('')
  const [creating, setCreating] = useState(false)
  const [selectedHook, setSelectedHook] = useState<string | null>(null)
  const [deliveries, setDeliveries] = useState<WebhookDelivery[]>([])
  const [testing, setTesting] = useState<string | null>(null)

  useEffect(() => { load() }, [])

  async function load() {
    try {
      const res = await apiClient.get<{ items: Webhook[] }>('/api/v1/webhooks')
      setWebhooks(res.items)
    } catch { /* ignore */ }
    setLoading(false)
  }

  async function handleCreate() {
    if (!newUrl.trim()) return
    setCreating(true)
    try {
      await apiClient.post('/api/v1/webhooks', {
        url: newUrl.trim(),
        secret: newSecret.trim() || null,
        events: newEvents.split(',').map(e => e.trim()).filter(Boolean),
      })
      setNewUrl('')
      setNewSecret('')
      setNewEvents('notification.sent,notification.failed')
      setShowCreate(false)
      await load()
    } catch { /* ignore */ }
    setCreating(false)
  }

  async function handleDelete(id: string) {
    if (!confirm('Delete this webhook?')) return
    await apiClient.delete(`/api/v1/webhooks/${id}`)
    await load()
  }

  async function loadDeliveries(id: string) {
    setSelectedHook(selectedHook === id ? null : id)
    if (selectedHook === id) return
    try {
      const res = await apiClient.get<{ items: WebhookDelivery[] }>(`/api/v1/webhooks/${id}/deliveries`)
      setDeliveries(res.items)
    } catch { /* ignore */ }
  }

  async function handleTest(id: string) {
    setTesting(id)
    try {
      await apiClient.post(`/api/v1/webhooks/${id}/test`)
      await loadDeliveries(id)
    } catch { /* ignore */ }
    setTesting(null)
  }

  const allEvents = ['notification.sent', 'notification.delivered', 'notification.failed', 'notification.bounced', 'notification.retrying', 'webhook.test']

  return (
    <AppLayout>
      <div className="mb-8 flex items-center justify-between">
        <div>
          <p className="hand text-xl text-violet mb-1">real-time alerts —</p>
          <h1 className="font-display font-bold text-3xl">Webhooks</h1>
        </div>
        <button
          onClick={() => setShowCreate(!showCreate)}
          className="bg-violet text-white px-4 py-2 rounded-full text-sm font-semibold hover:bg-violet/80 transition"
        >
          {showCreate ? 'Cancel' : '+ Add Webhook'}
        </button>
      </div>

      {showCreate && (
        <div className="bg-white border border-ink/10 rounded-xl p-6 mb-6">
          <h3 className="font-display font-bold text-lg mb-4">New Webhook</h3>
          <div className="space-y-4">
            <div>
              <label className="block text-xs font-medium text-ink/50 mb-1">Endpoint URL</label>
              <input
                type="url"
                value={newUrl}
                onChange={e => setNewUrl(e.target.value)}
                placeholder="https://yourapp.com/webhooks/notificationhub"
                className="w-full border border-ink/20 rounded-lg px-3 py-2 text-sm bg-white focus:outline-none focus:ring-2 focus:ring-violet"
              />
            </div>
            <div>
              <label className="block text-xs font-medium text-ink/50 mb-1">Events (comma-separated)</label>
              <input
                type="text"
                value={newEvents}
                onChange={e => setNewEvents(e.target.value)}
                className="w-full border border-ink/20 rounded-lg px-3 py-2 text-sm bg-white focus:outline-none focus:ring-2 focus:ring-violet"
              />
              <div className="flex flex-wrap gap-1 mt-2">
                {allEvents.map(ev => (
                  <button
                    key={ev}
                    onClick={() => setNewEvents(newEvents.includes(ev) ? newEvents : `${newEvents},${ev}`)}
                    className="text-xs px-2 py-0.5 rounded-full bg-fog border border-ink/10 text-ink/60 hover:bg-violet/10 hover:text-violet transition"
                  >
                    {ev}
                  </button>
                ))}
              </div>
            </div>
            <div>
              <label className="block text-xs font-medium text-ink/50 mb-1">Secret (optional, for HMAC signing)</label>
              <input
                type="text"
                value={newSecret}
                onChange={e => setNewSecret(e.target.value)}
                placeholder="whsec_..."
                className="w-full border border-ink/20 rounded-lg px-3 py-2 text-sm bg-white focus:outline-none focus:ring-2 focus:ring-violet"
              />
            </div>
            <button
              onClick={handleCreate}
              disabled={creating || !newUrl.trim()}
              className="bg-violet text-white px-4 py-2 rounded-lg text-sm font-semibold hover:bg-violet/80 transition disabled:opacity-50"
            >
              {creating ? 'Creating...' : 'Create Webhook'}
            </button>
          </div>
        </div>
      )}

      {loading ? (
        <div className="text-center py-12 text-ink/40 text-sm">Loading...</div>
      ) : webhooks.length === 0 ? (
        <div className="bg-white border border-ink/10 rounded-xl p-12 text-center">
          <p className="font-display font-bold text-lg mb-2">No webhooks yet</p>
          <p className="text-sm text-ink/50 mb-4">Get notified in real-time when email status changes.</p>
          <button
            onClick={() => setShowCreate(true)}
            className="bg-violet text-white px-4 py-2 rounded-full text-sm font-semibold hover:bg-violet/80 transition"
          >
            Create your first webhook
          </button>
        </div>
      ) : (
        <div className="space-y-4">
          {webhooks.map(hook => (
            <div key={hook.id} className="bg-white border border-ink/10 rounded-xl p-5">
              <div className="flex items-start justify-between">
                <div className="min-w-0 flex-1">
                  <div className="flex items-center gap-2 mb-1">
                    <span className={`w-2 h-2 rounded-full ${hook.isActive ? 'bg-teal' : 'bg-ink/20'}`} />
                    <code className="text-sm font-mono text-ink/70 truncate">{hook.url}</code>
                  </div>
                  <div className="flex flex-wrap gap-1 mt-2">
                    {hook.events.map(ev => (
                      <span key={ev} className="text-xs px-2 py-0.5 rounded-full bg-violet/10 text-violet">{ev}</span>
                    ))}
                  </div>
                  {hook.lastError && (
                    <p className="text-xs text-coral mt-2">Last error: {hook.lastError}</p>
                  )}
                </div>
                <div className="flex items-center gap-2 ml-4 shrink-0">
                  <button
                    onClick={() => handleTest(hook.id)}
                    disabled={testing === hook.id}
                    className="text-xs px-3 py-1.5 border border-ink/20 rounded-lg hover:bg-fog transition disabled:opacity-50"
                  >
                    {testing === hook.id ? 'Sending...' : 'Test'}
                  </button>
                  <button
                    onClick={() => loadDeliveries(hook.id)}
                    className="text-xs px-3 py-1.5 border border-ink/20 rounded-lg hover:bg-fog transition"
                  >
                    {selectedHook === hook.id ? 'Hide' : 'Deliveries'}
                  </button>
                  <button
                    onClick={() => handleDelete(hook.id)}
                    className="text-xs px-3 py-1.5 border border-coral/30 text-coral rounded-lg hover:bg-coral/10 transition"
                  >
                    Delete
                  </button>
                </div>
              </div>

              {selectedHook === hook.id && deliveries.length > 0 && (
                <div className="mt-4 pt-4 border-t border-ink/10">
                  <p className="text-xs font-medium text-ink/50 mb-2">Recent Deliveries</p>
                  <div className="space-y-1">
                    {deliveries.map(d => (
                      <div key={d.id} className="flex items-center gap-3 text-xs">
                        <span className={`w-1.5 h-1.5 rounded-full ${d.isSuccess ? 'bg-teal' : 'bg-coral'}`} />
                        <span className="font-mono text-ink/60">{d.event}</span>
                        <span className={d.isSuccess ? 'text-teal' : 'text-coral'}>HTTP {d.statusCode}</span>
                        <span className="text-ink/40">{d.durationMs.toFixed(0)}ms</span>
                        <span className="text-ink/40">{new Date(d.createdAt).toLocaleString()}</span>
                      </div>
                    ))}
                  </div>
                </div>
              )}
            </div>
          ))}
        </div>
      )}

      <div className="mt-8 bg-fog border border-ink/10 rounded-xl p-6">
        <h3 className="font-display font-bold text-sm mb-2">How webhooks work</h3>
        <ul className="text-xs text-ink/50 space-y-1">
          <li>• We POST a JSON payload to your endpoint when an event occurs</li>
          <li>• Include a secret to get an <code className="text-violet">X-Webhook-Signature</code> HMAC header</li>
          <li>• Retry delivery up to 3 times on failure</li>
          <li>• View delivery history and response codes for each webhook</li>
        </ul>
      </div>
    </AppLayout>
  )
}
