import { useState, useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import AppLayout from '@/app/layouts/AppLayout'
import { apiClient } from '@/shared/services/apiClient'

interface Template {
  id: string
  name: string
  subject: string
  createdAt: string
}

interface TemplateForm {
  name: string
  subject: string
  body: string
}

type EditorTab = 'html' | 'preview'

const STARTER_HTML = `<!DOCTYPE html>
<html>
<head>
  <meta charset="utf-8" />
  <style>
    body { font-family: Arial, sans-serif; background: #f6f5f2; margin: 0; padding: 0; }
    .container { max-width: 600px; margin: 40px auto; background: #fff; border-radius: 12px; overflow: hidden; }
    .header { background: #6D28D9; padding: 32px; text-align: center; }
    .header h1 { color: #fff; margin: 0; font-size: 24px; }
    .body { padding: 32px; color: #15131B; line-height: 1.6; }
    .body img { max-width: 100%; border-radius: 8px; margin: 16px 0; }
    .button { display: inline-block; background: #6D28D9; color: #fff; padding: 12px 28px; border-radius: 999px; text-decoration: none; font-weight: 600; margin-top: 16px; }
    .footer { padding: 20px 32px; text-align: center; font-size: 12px; color: #999; border-top: 1px solid #eee; }
  </style>
</head>
<body>
  <div class="container">
    <div class="header">
      <h1>Welcome, {{FirstName}}!</h1>
    </div>
    <div class="body">
      <p>Hi {{FirstName}},</p>
      <p>Thanks for joining <strong>{{AppName}}</strong>. We're excited to have you on board.</p>
      <img src="{{BannerUrl}}" alt="Welcome banner" />
      <p>Click below to get started:</p>
      <a href="{{CtaUrl}}" class="button">Get Started</a>
    </div>
    <div class="footer">
      You're receiving this because you signed up for {{AppName}}.
    </div>
  </div>
</body>
</html>`

export default function TemplatesPage() {
  const navigate = useNavigate()
  const [templates, setTemplates] = useState<Template[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [showForm, setShowForm] = useState(false)
  const [form, setForm] = useState<TemplateForm>({ name: '', subject: '', body: STARTER_HTML })
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [activeTab, setActiveTab] = useState<EditorTab>('html')

  async function load() {
    try {
      const res = await apiClient.get<{ items: Template[] }>('/api/v1/templates')
      setTemplates(res.items)
    } catch {
      // fail silently
    } finally {
      setIsLoading(false)
    }
  }

  useEffect(() => { load() }, [])

  async function handleSubmit() {
    if (!form.name || !form.subject || !form.body) {
      setError('All fields are required.')
      return
    }
    setSaving(true)
    setError(null)
    try {
      await apiClient.post('/api/v1/templates', form)
      setShowForm(false)
      setForm({ name: '', subject: '', body: STARTER_HTML })
      await load()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to save template.')
    } finally {
      setSaving(false)
    }
  }

  async function handleDelete(id: string) {
    if (!confirm('Delete this template?')) return
    try {
      await apiClient.delete(`/api/v1/templates/${id}`)
      await load()
    } catch {
      // fail silently
    }
  }

  function openNew() {
    setShowForm(true)
    setForm({ name: '', subject: '', body: STARTER_HTML })
    setActiveTab('html')
    setError(null)
  }

  return (
    <AppLayout>
      <div className="flex items-center justify-between mb-6">
        <div>
          <p className="hand text-xl text-violet mb-1">reusable designs —</p>
          <h1 className="font-display font-bold text-3xl">Templates</h1>
          <p className="text-ink/50 text-sm mt-1">
            Write HTML + CSS, use {`{{Placeholder}}`} variables, embed images by URL
          </p>
        </div>
        {!showForm && (
          <button
            onClick={openNew}
            className="px-4 py-2 bg-ink text-white rounded-lg text-sm font-medium hover:bg-ink/80 transition"
          >
            + New Template
          </button>
        )}
      </div>

      {/* New template form */}
      {showForm && (
        <div className="bg-white border border-ink/10 rounded-xl mb-6 overflow-hidden">
          <div className="flex items-center justify-between px-6 py-4 border-b border-ink/10">
            <h2 className="font-display font-bold text-lg">New Template</h2>
            <button
              onClick={() => { setShowForm(false); setError(null) }}
              className="text-ink/40 hover:text-ink text-sm"
            >
              Cancel
            </button>
          </div>

          <div className="p-6 space-y-4">
            <div className="grid sm:grid-cols-2 gap-4">
              <div>
                <label className="block text-sm font-medium mb-1">Template Name</label>
                <input
                  className="w-full border border-ink/20 rounded-lg px-3 py-2 text-sm"
                  placeholder="e.g. WelcomeEmail"
                  value={form.name}
                  onChange={e => setForm(f => ({ ...f, name: e.target.value }))}
                />
              </div>
              <div>
                <label className="block text-sm font-medium mb-1">Email Subject</label>
                <input
                  className="w-full border border-ink/20 rounded-lg px-3 py-2 text-sm"
                  placeholder="e.g. Welcome to {{AppName}}!"
                  value={form.subject}
                  onChange={e => setForm(f => ({ ...f, subject: e.target.value }))}
                />
              </div>
            </div>

            {/* Editor tabs */}
            <div>
              <div className="flex items-center gap-1 mb-2 border-b border-ink/10">
                <button
                  onClick={() => setActiveTab('html')}
                  className={`px-4 py-2 text-sm font-medium border-b-2 -mb-px transition ${
                    activeTab === 'html'
                      ? 'border-violet text-violet'
                      : 'border-transparent text-ink/50 hover:text-ink'
                  }`}
                >
                  HTML Editor
                </button>
                <button
                  onClick={() => setActiveTab('preview')}
                  className={`px-4 py-2 text-sm font-medium border-b-2 -mb-px transition ${
                    activeTab === 'preview'
                      ? 'border-violet text-violet'
                      : 'border-transparent text-ink/50 hover:text-ink'
                  }`}
                >
                  Preview
                </button>
                <div className="ml-auto pb-2">
                  <span className="text-xs text-ink/30 font-mono">
                    {`{{Placeholder}}`} variables supported
                  </span>
                </div>
              </div>

              {activeTab === 'html' ? (
                <div>
                  <textarea
                    className="w-full border border-ink/20 rounded-lg px-4 py-3 text-xs font-mono leading-relaxed bg-ink text-paper resize-none focus:outline-none focus:ring-1 focus:ring-violet"
                    rows={24}
                    spellCheck={false}
                    placeholder="Paste or write your HTML email here..."
                    value={form.body}
                    onChange={e => setForm(f => ({ ...f, body: e.target.value }))}
                  />
                  <div className="mt-2 flex flex-wrap gap-2">
                    {['{{FirstName}}', '{{AppName}}', '{{CtaUrl}}', '{{BannerUrl}}'].map(v => (
                      <button
                        key={v}
                        onClick={() => setForm(f => ({ ...f, body: f.body + v }))}
                        className="text-xs px-2 py-1 bg-violet/10 text-violet rounded font-mono hover:bg-violet/20 transition"
                      >
                        {v}
                      </button>
                    ))}
                    <span className="text-xs text-ink/30 self-center ml-1">
                      Click to insert variable
                    </span>
                  </div>
                </div>
              ) : (
                <div className="border border-ink/10 rounded-lg overflow-hidden bg-fog">
                  <div className="px-3 py-2 bg-fog border-b border-ink/10 flex items-center gap-2">
                    <span className="w-2.5 h-2.5 rounded-full bg-coral/60" />
                    <span className="w-2.5 h-2.5 rounded-full bg-yellow/60" />
                    <span className="w-2.5 h-2.5 rounded-full bg-teal/60" />
                    <span className="text-xs text-ink/30 ml-2 font-mono">email preview</span>
                  </div>
                  <iframe
                    srcDoc={form.body}
                    className="w-full bg-white"
                    style={{ height: '520px', border: 'none' }}
                    title="Email preview"
                    sandbox="allow-same-origin"
                  />
                </div>
              )}
            </div>

            {error && <p className="text-sm text-coral">{error}</p>}

            <div className="flex gap-3 pt-2">
              <button
                onClick={handleSubmit}
                disabled={saving}
                className="px-5 py-2.5 bg-ink text-white rounded-lg text-sm font-medium disabled:opacity-50 hover:bg-ink/80 transition"
              >
                {saving ? 'Saving...' : 'Save Template'}
              </button>
              <button
                onClick={() => { setShowForm(false); setError(null) }}
                className="px-5 py-2.5 border border-ink/20 rounded-lg text-sm font-medium hover:bg-fog transition"
              >
                Cancel
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Template list */}
      {isLoading ? (
        <p className="text-ink/50 text-sm">Loading...</p>
      ) : templates.length === 0 ? (
        <div className="text-center py-20 text-ink/40">
          <p className="text-lg font-medium mb-1">No templates yet</p>
          <p className="text-sm">Create your first HTML email template to get started.</p>
        </div>
      ) : (
        <div className="bg-white border border-ink/10 rounded-xl overflow-hidden">
          <table className="w-full text-sm">
            <thead className="bg-fog/40 border-b border-ink/10">
              <tr>
                <th className="text-left px-4 py-3 font-medium">Name</th>
                <th className="text-left px-4 py-3 font-medium">Subject</th>
                <th className="text-left px-4 py-3 font-medium">Created</th>
                <th className="text-left px-4 py-3 font-medium">Actions</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-ink/5">
              {templates.map(t => (
                <tr
                  key={t.id}
                  className="hover:bg-fog/20 transition cursor-pointer"
                  onClick={() => navigate(`/templates/${t.id}`)}
                >
                  <td className="px-4 py-3 font-medium">{t.name}</td>
                  <td className="px-4 py-3 text-ink/60 truncate max-w-xs">{t.subject}</td>
                  <td className="px-4 py-3 text-ink/40 text-xs">
                    {new Date(t.createdAt).toLocaleDateString()}
                  </td>
                  <td className="px-4 py-3" onClick={e => e.stopPropagation()}>
                    <div className="flex gap-2">
                      <button
                        onClick={() => navigate(`/templates/${t.id}`)}
                        className="text-xs px-3 py-1 border border-ink/20 rounded-lg hover:bg-fog transition"
                      >
                        Edit
                      </button>
                      <button
                        onClick={() => handleDelete(t.id)}
                        className="text-xs px-3 py-1 border border-coral/30 text-coral rounded-lg hover:bg-red-50 transition"
                      >
                        Delete
                      </button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </AppLayout>
  )
}