import { useState, useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import AppLayout from '@/app/layouts/AppLayout'
import { useCampaigns } from '../hooks/useCampaigns'
import { campaignApi } from '../api/campaignApi'
import { apiClient } from '@/shared/services/apiClient'
import type { CampaignStatus } from '../types/campaign.types'

const statusStyle: Record<CampaignStatus, string> = {
  Draft: 'bg-ink/10 text-ink/50',
  Scheduled: 'bg-violet/10 text-violet',
  Running: 'bg-yellow/20 text-ink',
  Sent: 'bg-teal/10 text-teal',
  Paused: 'bg-coral/10 text-coral',
  Completed: 'bg-teal/10 text-teal',
  Cancelled: 'bg-ink/10 text-ink/40',
}

interface Template {
  id: string
  name: string
  subject: string
}

export default function CampaignsPage() {
  const navigate = useNavigate()
  const { campaigns, totalCount, isLoading, error, reload } = useCampaigns()
  const [showForm, setShowForm] = useState(false)
  const [form, setForm] = useState({ title: '', subject: '', body: '' })
  const [creating, setCreating] = useState(false)
  const [createError, setCreateError] = useState<string | null>(null)
  const [actionError, setActionError] = useState<string | null>(null)
  const [templates, setTemplates] = useState<Template[]>([])
  const [selectedTemplateId, setSelectedTemplateId] = useState<string>('')
  const [bodyMode, setBodyMode] = useState<'template' | 'custom'>('template')

  useEffect(() => {
    apiClient.get<{ items: Template[] }>('/api/v1/templates')
      .then(res => setTemplates(res.items))
      .catch(() => {})
  }, [])

  const sent = campaigns.filter(c => c.status === 'Completed' || c.status === 'Sent').length
  const scheduled = campaigns.filter(c => c.status === 'Scheduled').length
  const totalReached = campaigns
    .filter(c => c.status === 'Completed' || c.status === 'Sent')
    .reduce((acc, c) => acc + c.totalRecipients, 0)

  async function handleCreate(e: React.FormEvent) {
    e.preventDefault()
    setCreating(true)
    setCreateError(null)
    try {
      const result = await campaignApi.create({
        title: form.title,
        subject: form.subject,
        body: bodyMode === 'custom' ? form.body : undefined,
        templateId: bodyMode === 'template' && selectedTemplateId ? selectedTemplateId : undefined,
        channel: 0,
      })
      setShowForm(false)
      setForm({ title: '', subject: '', body: '' })
      setSelectedTemplateId('')
      setBodyMode('template')
      navigate(`/campaigns/${result.id}`)
    } catch (err) {
      setCreateError(err instanceof Error ? err.message : 'Failed to create campaign.')
    } finally {
      setCreating(false)
    }
  }

  async function handleAction(id: string, action: 'send' | 'pause' | 'resume' | 'delete') {
    setActionError(null)
    try {
      if (action === 'send') await campaignApi.send(id)
      else if (action === 'pause') await campaignApi.pause(id)
      else if (action === 'resume') await campaignApi.resume(id)
      else if (action === 'delete') await campaignApi.delete(id)
      await reload()
    } catch (err) {
      setActionError(err instanceof Error ? err.message : 'Action failed.')
    }
  }

  return (
    <AppLayout>
      <div className="mb-8 flex items-center justify-between">
        <div>
          <p className="hand text-xl text-violet mb-1">bulk sending —</p>
          <h1 className="font-display font-bold text-3xl">Campaigns</h1>
        </div>
        <button
          onClick={() => setShowForm(true)}
          className="px-4 py-2 bg-ink text-white rounded-lg text-sm font-medium hover:bg-violet transition"
        >
          + New Campaign
        </button>
      </div>

      <div className="grid grid-cols-3 gap-4 mb-8">
        <div className="bg-fog border border-ink/10 rounded-xl p-5 rotate-[-0.5deg]">
          <p className="font-display font-extrabold text-3xl text-teal">{sent}</p>
          <p className="text-xs text-ink/50 mt-1">Campaigns Sent</p>
        </div>
        <div className="bg-fog border border-ink/10 rounded-xl p-5 rotate-[0.5deg]">
          <p className="font-display font-extrabold text-3xl text-violet">{scheduled}</p>
          <p className="text-xs text-ink/50 mt-1">Scheduled</p>
        </div>
        <div className="bg-fog border border-ink/10 rounded-xl p-5 rotate-[-0.3deg]">
          <p className="font-display font-extrabold text-3xl text-ink">
            {totalReached.toLocaleString()}
          </p>
          <p className="text-xs text-ink/50 mt-1">Total Recipients Reached</p>
        </div>
      </div>

      {actionError && (
        <div className="bg-coral/10 text-coral text-sm px-4 py-3 rounded-xl mb-4">{actionError}</div>
      )}

      {showForm && (
        <div className="bg-white border border-ink/10 rounded-xl p-6 mb-6">
          <div className="flex items-center justify-between mb-4">
            <h2 className="font-display font-bold text-lg">New Campaign</h2>
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
                <label className="block text-sm font-medium mb-1">Campaign Name</label>
                <input
                  className="w-full border border-ink/20 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-violet"
                  placeholder="e.g. July Product Update"
                  value={form.title}
                  onChange={e => setForm(f => ({ ...f, title: e.target.value }))}
                  required
                />
              </div>
              <div>
                <label className="block text-sm font-medium mb-1">Subject Line</label>
                <input
                  className="w-full border border-ink/20 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-violet"
                  placeholder="e.g. What's new this month"
                  value={form.subject}
                  onChange={e => setForm(f => ({ ...f, subject: e.target.value }))}
                  required
                />
              </div>
            </div>

            {/* Body mode toggle */}
            <div>
              <label className="block text-sm font-medium mb-2">Email Body</label>
              <div className="flex items-center bg-fog rounded-lg p-1 gap-1 w-fit mb-3">
                <button
                  type="button"
                  onClick={() => setBodyMode('template')}
                  className={`px-3 py-1.5 rounded-md text-xs font-medium transition ${
                    bodyMode === 'template' ? 'bg-white shadow-sm text-ink' : 'text-ink/50 hover:text-ink'
                  }`}
                >
                  Use Template
                </button>
                <button
                  type="button"
                  onClick={() => setBodyMode('custom')}
                  className={`px-3 py-1.5 rounded-md text-xs font-medium transition ${
                    bodyMode === 'custom' ? 'bg-white shadow-sm text-ink' : 'text-ink/50 hover:text-ink'
                  }`}
                >
                  Write Custom
                </button>
              </div>

              {bodyMode === 'template' ? (
                templates.length === 0 ? (
                  <div className="border-2 border-dashed border-ink/15 rounded-xl p-8 text-center text-ink/40 text-sm">
                    No templates yet —{' '}
                    <a href="/templates" className="text-violet underline">create one first</a>
                  </div>
                ) : (
                  <div className="grid sm:grid-cols-2 gap-3 max-h-64 overflow-y-auto">
                    {templates.map(t => (
                      <div
                        key={t.id}
                        onClick={() => setSelectedTemplateId(t.id)}
                        className={`border rounded-xl p-4 cursor-pointer transition ${
                          selectedTemplateId === t.id
                            ? 'border-violet bg-violet/5'
                            : 'border-ink/10 hover:border-ink/30 bg-fog/30'
                        }`}
                      >
                        <div className="flex items-start justify-between gap-2">
                          <div className="min-w-0">
                            <p className="text-sm font-medium truncate">{t.name}</p>
                            <p className="text-xs text-ink/40 mt-0.5 truncate">{t.subject}</p>
                          </div>
                          {selectedTemplateId === t.id && (
                            <span className="shrink-0 text-xs bg-violet text-white px-2 py-0.5 rounded-full">
                              Selected
                            </span>
                          )}
                        </div>
                      </div>
                    ))}
                  </div>
                )
              ) : (
                <textarea
                  className="w-full border border-ink/20 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-violet h-32 resize-none"
                  placeholder="Write your message here..."
                  value={form.body}
                  onChange={e => setForm(f => ({ ...f, body: e.target.value }))}
                />
              )}
            </div>

            <div className="flex gap-3 pt-2">
              <button
                type="submit"
                disabled={
                  creating ||
                  (bodyMode === 'template' && !selectedTemplateId) ||
                  (bodyMode === 'custom' && !form.body.trim())
                }
                className="px-4 py-2 bg-ink text-white rounded-lg text-sm font-medium hover:bg-violet transition disabled:opacity-50"
              >
                {creating ? 'Creating...' : 'Create Draft'}
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
      ) : campaigns.length === 0 ? (
        <div className="text-center py-16 text-ink/40">
          <p className="text-lg font-medium mb-1">No campaigns yet</p>
          <p className="text-sm">Create your first campaign to start sending bulk notifications.</p>
        </div>
      ) : (
        <div className="bg-fog border border-ink/10 rounded-xl overflow-hidden">
          <table className="w-full text-sm">
            <thead className="border-b border-ink/10">
              <tr className="text-left text-ink/50 text-xs uppercase tracking-wide">
                <th className="px-4 py-3 font-medium">Campaign</th>
                <th className="px-4 py-3 font-medium">Status</th>
                <th className="px-4 py-3 font-medium">Recipients</th>
                <th className="px-4 py-3 font-medium">Scheduled</th>
                <th className="px-4 py-3 font-medium">Created</th>
                <th className="px-4 py-3 font-medium">Actions</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-ink/5">
              {campaigns.map(c => (
                <tr key={c.id} className="hover:bg-fog/50 transition">
                  <td
                    className="px-4 py-3 cursor-pointer"
                    onClick={() => navigate(`/campaigns/${c.id}`)}
                  >
                    <p className="font-medium">{c.title}</p>
                    <p className="text-xs text-ink/40 mt-0.5 truncate max-w-xs">{c.subject}</p>
                  </td>
                  <td className="px-4 py-3">
                    <span className={`text-xs px-2 py-0.5 rounded-full font-medium ${statusStyle[c.status]}`}>
                      {c.status}
                    </span>
                  </td>
                  <td className="px-4 py-3 text-ink/60">
                    {c.totalRecipients > 0 ? c.totalRecipients.toLocaleString() : '—'}
                  </td>
                  <td className="px-4 py-3 text-ink/40 text-xs">
                    {c.scheduledAt ? new Date(c.scheduledAt).toLocaleString() : '—'}
                  </td>
                  <td className="px-4 py-3 text-ink/40 text-xs">
                    {new Date(c.createdAt).toLocaleDateString()}
                  </td>
                  <td className="px-4 py-3">
                    <div className="flex gap-2">
                      {c.status === 'Draft' && (
                        <button
                          onClick={() => handleAction(c.id, 'send')}
                          className="text-xs px-2 py-1 bg-teal/10 text-teal rounded hover:bg-teal/20 transition"
                        >
                          Send
                        </button>
                      )}
                      {c.status === 'Running' && (
                        <button
                          onClick={() => handleAction(c.id, 'pause')}
                          className="text-xs px-2 py-1 bg-yellow/20 text-ink rounded hover:bg-yellow/30 transition"
                        >
                          Pause
                        </button>
                      )}
                      {c.status === 'Paused' && (
                        <button
                          onClick={() => handleAction(c.id, 'resume')}
                          className="text-xs px-2 py-1 bg-violet/10 text-violet rounded hover:bg-violet/20 transition"
                        >
                          Resume
                        </button>
                      )}
                      {c.status !== 'Running' && (
                        <button
                          onClick={() => handleAction(c.id, 'delete')}
                          className="text-xs px-2 py-1 bg-coral/10 text-coral rounded hover:bg-coral/20 transition"
                        >
                          Delete
                        </button>
                      )}
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
          <div className="px-4 py-3 border-t border-ink/10 text-xs text-ink/40">
            {totalCount} campaign{totalCount !== 1 ? 's' : ''} total
          </div>
        </div>
      )}
    </AppLayout>
  )
}