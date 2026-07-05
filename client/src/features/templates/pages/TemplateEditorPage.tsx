import { useState, useEffect } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import AppLayout from '@/app/layouts/AppLayout'
import { apiClient } from '@/shared/services/apiClient'

type EditorTab = 'html' | 'preview'

export default function TemplateEditorPage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()

  const [form, setForm] = useState({ name: '', subject: '', body: '' })
  const [isLoading, setIsLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [saved, setSaved] = useState(false)
  const [activeTab, setActiveTab] = useState<EditorTab>('html')

  useEffect(() => {
    if (!id) return
    apiClient.get<{ id: string; name: string; subject: string; body: string }>(
      `/api/v1/templates/${id}`
    )
      .then(res => setForm({ name: res.name, subject: res.subject, body: res.body }))
      .catch(() => setError('Template not found.'))
      .finally(() => setIsLoading(false))
  }, [id])

  async function handleSave() {
    setSaving(true)
    setError(null)
    setSaved(false)
    try {
      await apiClient.put(`/api/v1/templates/${id}`, form)
      setSaved(true)
      setTimeout(() => setSaved(false), 3000)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to save.')
    } finally {
      setSaving(false)
    }
  }

  const placeholders = ['{{FirstName}}', '{{AppName}}', '{{CtaUrl}}', '{{BannerUrl}}', '{{UnsubscribeUrl}}']

  function insertPlaceholder(v: string) {
    setForm(f => ({ ...f, body: f.body + v }))
  }

  if (isLoading) {
    return (
      <AppLayout>
        <p className="text-ink/40 text-sm">Loading template...</p>
      </AppLayout>
    )
  }

  return (
    <AppLayout>
      {/* Header */}
      <div className="flex items-center justify-between mb-6">
        <div>
          <button
            onClick={() => navigate('/templates')}
            className="text-xs text-ink/40 hover:text-ink mb-2 flex items-center gap-1 transition"
          >
            ← Templates
          </button>
          <h1 className="font-display font-bold text-2xl">{form.name || 'Untitled Template'}</h1>
        </div>
        <div className="flex items-center gap-3">
          {saved && (
            <span className="text-xs text-teal font-medium">Saved</span>
          )}
          {error && (
            <span className="text-xs text-coral">{error}</span>
          )}
          <button
            onClick={handleSave}
            disabled={saving}
            className="px-5 py-2.5 bg-ink text-white rounded-lg text-sm font-medium disabled:opacity-50 hover:bg-ink/80 transition"
          >
            {saving ? 'Saving...' : 'Save'}
          </button>
        </div>
      </div>

      {/* Name + Subject */}
      <div className="grid sm:grid-cols-2 gap-4 mb-6">
        <div>
          <label className="block text-xs font-medium text-ink/50 mb-1 uppercase tracking-wide">Template Name</label>
          <input
            className="w-full border border-ink/20 rounded-lg px-3 py-2 text-sm"
            value={form.name}
            onChange={e => setForm(f => ({ ...f, name: e.target.value }))}
          />
        </div>
        <div>
          <label className="block text-xs font-medium text-ink/50 mb-1 uppercase tracking-wide">Email Subject</label>
          <input
            className="w-full border border-ink/20 rounded-lg px-3 py-2 text-sm"
            placeholder="e.g. Welcome to {{AppName}}!"
            value={form.subject}
            onChange={e => setForm(f => ({ ...f, subject: e.target.value }))}
          />
        </div>
      </div>

      {/* Editor + Preview split */}
      <div className="grid lg:grid-cols-2 gap-4">
        {/* Left — HTML editor */}
        <div className="flex flex-col">
          <div className="flex items-center justify-between mb-2">
            <span className="text-xs font-medium text-ink/50 uppercase tracking-wide">HTML Editor</span>
            <div className="flex gap-1 flex-wrap">
              {placeholders.map(v => (
                <button
                  key={v}
                  onClick={() => insertPlaceholder(v)}
                  className="text-xs px-2 py-0.5 bg-violet/10 text-violet rounded font-mono hover:bg-violet/20 transition"
                >
                  {v}
                </button>
              ))}
            </div>
          </div>
          <textarea
            className="flex-1 border border-ink/20 rounded-xl px-4 py-3 text-xs font-mono leading-relaxed bg-ink text-paper resize-none focus:outline-none focus:ring-1 focus:ring-violet"
            style={{ minHeight: '600px' }}
            spellCheck={false}
            value={form.body}
            onChange={e => setForm(f => ({ ...f, body: e.target.value }))}
          />
        </div>

        {/* Right — Live preview */}
        <div className="flex flex-col">
          <div className="flex items-center gap-2 mb-2">
            <span className="text-xs font-medium text-ink/50 uppercase tracking-wide">Live Preview</span>
            <span className="text-xs text-ink/30">— updates as you type</span>
          </div>
          <div className="border border-ink/10 rounded-xl overflow-hidden flex-1">
            <div className="px-3 py-2 bg-fog border-b border-ink/10 flex items-center gap-2">
              <span className="w-2.5 h-2.5 rounded-full bg-coral/60" />
              <span className="w-2.5 h-2.5 rounded-full bg-yellow/60" />
              <span className="w-2.5 h-2.5 rounded-full bg-teal/60" />
              <span className="text-xs text-ink/30 ml-2 font-mono">email preview</span>
            </div>
            <iframe
              srcDoc={form.body}
              className="w-full bg-white"
              style={{ height: '560px', border: 'none' }}
              title="Email preview"
              sandbox="allow-same-origin"
            />
          </div>
        </div>
      </div>
    </AppLayout>
  )
}