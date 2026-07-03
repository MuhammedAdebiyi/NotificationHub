import { useState, useEffect } from 'react'
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

export default function TemplatesPage() {
  const [templates, setTemplates] = useState<Template[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [showForm, setShowForm] = useState(false)
  const [editingId, setEditingId] = useState<string | null>(null)
  const [form, setForm] = useState<TemplateForm>({ name: '', subject: '', body: '' })
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)

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
      if (editingId) {
        await apiClient.put(`/api/v1/templates/${editingId}`, form)
      } else {
        await apiClient.post('/api/v1/templates', form)
      }
      setShowForm(false)
      setEditingId(null)
      setForm({ name: '', subject: '', body: '' })
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

  async function handleEdit(id: string) {
    try {
      const res = await apiClient.get<{ id: string; name: string; subject: string; body: string }>(
        `/api/v1/templates/${id}`
      )
      setForm({ name: res.name, subject: res.subject, body: res.body })
      setEditingId(id)
      setShowForm(true)
    } catch {
      // fail silently
    }
  }

  return (
    <AppLayout>
      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="font-display font-bold text-3xl mb-1">Templates</h1>
          <p className="text-ink/50 text-sm">Reusable email templates with {`{{placeholder}}`} support</p>
        </div>
        <button
          onClick={() => { setShowForm(true); setEditingId(null); setForm({ name: '', subject: '', body: '' }) }}
          className="px-4 py-2 bg-ink text-white rounded-lg text-sm font-medium hover:bg-ink/80 transition"
        >
          + New Template
        </button>
      </div>

      {showForm && (
        <div className="bg-white border border-ink/10 rounded-xl p-6 mb-6">
          <h2 className="font-display font-bold text-lg mb-4">
            {editingId ? 'Edit Template' : 'New Template'}
          </h2>
          <div className="space-y-4">
            <div>
              <label className="block text-sm font-medium mb-1">Name</label>
              <input
                className="w-full border border-ink/20 rounded-lg px-3 py-2 text-sm"
                placeholder="e.g. WelcomeEmail"
                value={form.name}
                onChange={e => setForm(f => ({ ...f, name: e.target.value }))}
              />
            </div>
            <div>
              <label className="block text-sm font-medium mb-1">Subject</label>
              <input
                className="w-full border border-ink/20 rounded-lg px-3 py-2 text-sm"
                placeholder="e.g. Welcome to {{AppName}}!"
                value={form.subject}
                onChange={e => setForm(f => ({ ...f, subject: e.target.value }))}
              />
            </div>
            <div>
              <label className="block text-sm font-medium mb-1">Body</label>
              <textarea
                className="w-full border border-ink/20 rounded-lg px-3 py-2 text-sm font-mono"
                rows={8}
                placeholder="Hi {{FirstName}}, welcome to {{AppName}}..."
                value={form.body}
                onChange={e => setForm(f => ({ ...f, body: e.target.value }))}
              />
              <p className="text-xs text-ink/40 mt-1">
                Use {`{{VariableName}}`} for placeholders. These get replaced when sending.
              </p>
            </div>
            {error && <p className="text-sm text-red-500">{error}</p>}
            <div className="flex gap-3">
              <button
                onClick={handleSubmit}
                disabled={saving}
                className="px-4 py-2 bg-ink text-white rounded-lg text-sm font-medium disabled:opacity-50"
              >
                {saving ? 'Saving...' : 'Save Template'}
              </button>
              <button
                onClick={() => { setShowForm(false); setError(null) }}
                className="px-4 py-2 border border-ink/20 rounded-lg text-sm font-medium"
              >
                Cancel
              </button>
            </div>
          </div>
        </div>
      )}

      {isLoading ? (
        <p className="text-ink/50 text-sm">Loading...</p>
      ) : templates.length === 0 ? (
        <div className="text-center py-16 text-ink/40">
          <p className="text-lg font-medium mb-1">No templates yet</p>
          <p className="text-sm">Create your first template to get started.</p>
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
                <tr key={t.id} className="hover:bg-fog/20 transition">
                  <td className="px-4 py-3 font-medium">{t.name}</td>
                  <td className="px-4 py-3 text-ink/60">{t.subject}</td>
                  <td className="px-4 py-3 text-ink/40">
                    {new Date(t.createdAt).toLocaleDateString()}
                  </td>
                  <td className="px-4 py-3">
                    <div className="flex gap-2">
                      <button
                        onClick={() => handleEdit(t.id)}
                        className="text-xs px-3 py-1 border border-ink/20 rounded-lg hover:bg-fog transition"
                      >
                        Edit
                      </button>
                      <button
                        onClick={() => handleDelete(t.id)}
                        className="text-xs px-3 py-1 border border-red-200 text-red-500 rounded-lg hover:bg-red-50 transition"
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