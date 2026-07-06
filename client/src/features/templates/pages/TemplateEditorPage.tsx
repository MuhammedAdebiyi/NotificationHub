import { useState, useEffect } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import AppLayout from '@/app/layouts/AppLayout'
import { apiClient } from '@/shared/services/apiClient'
import { useEditor, EditorContent } from '@tiptap/react'
import StarterKit from '@tiptap/starter-kit'
import { TextStyle } from '@tiptap/extension-text-style'
import { Color } from '@tiptap/extension-color'
import Image from '@tiptap/extension-image'
import TextAlign from '@tiptap/extension-text-align'
import Underline from '@tiptap/extension-underline'
import Link from '@tiptap/extension-link'

type EditorMode = 'html' | 'rich'

interface DeleteModalProps {
  name: string
  onConfirm: () => void
  onCancel: () => void
}

function DeleteModal({ name, onConfirm, onCancel }: DeleteModalProps) {
  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center">
      <div className="absolute inset-0 bg-ink/40 backdrop-blur-sm" onClick={onCancel} />
      <div className="relative bg-white rounded-2xl p-8 max-w-sm w-full mx-4 shadow-2xl">
        <div className="w-12 h-12 bg-coral/10 rounded-full flex items-center justify-center mb-4">
          <span className="text-coral text-xl">✕</span>
        </div>
        <h3 className="font-display font-bold text-lg mb-1">Delete template?</h3>
        <p className="text-sm text-ink/60 mb-6">
          <span className="font-medium text-ink">"{name}"</span> will be permanently deleted. This cannot be undone.
        </p>
        <div className="flex gap-3">
          <button
            onClick={onConfirm}
            className="flex-1 px-4 py-2.5 bg-coral text-white rounded-lg text-sm font-medium hover:bg-coral/80 transition"
          >
            Delete
          </button>
          <button
            onClick={onCancel}
            className="flex-1 px-4 py-2.5 border border-ink/20 rounded-lg text-sm font-medium hover:bg-fog transition"
          >
            Cancel
          </button>
        </div>
      </div>
    </div>
  )
}

function RichToolbar({ editor }: { editor: ReturnType<typeof useEditor> }) {
  if (!editor) return null

  const btn = (active: boolean) =>
    `px-2 py-1.5 rounded text-xs font-medium transition ${active ? 'bg-violet text-white' : 'hover:bg-fog text-ink/70'}`

  function addImage() {
    const url = window.prompt('Image URL')
    if (url) editor.chain().focus().setImage({ src: url }).run()
  }

  function setLink() {
    const url = window.prompt('Link URL')
    if (url) editor.chain().focus().setLink({ href: url }).run()
  }

  return (
    <div className="flex flex-wrap items-center gap-1 px-3 py-2 border-b border-ink/10 bg-fog/50">
      {/* Text style */}
      <button onClick={() => editor.chain().focus().toggleBold().run()} className={btn(editor.isActive('bold'))}>
        <strong>B</strong>
      </button>
      <button onClick={() => editor.chain().focus().toggleItalic().run()} className={btn(editor.isActive('italic'))}>
        <em>I</em>
      </button>
      <button onClick={() => editor.chain().focus().toggleUnderline().run()} className={btn(editor.isActive('underline'))}>
        <span className="underline">U</span>
      </button>
      <button onClick={() => editor.chain().focus().toggleStrike().run()} className={btn(editor.isActive('strike'))}>
        <span className="line-through">S</span>
      </button>

      <div className="w-px h-5 bg-ink/10 mx-1" />

      {/* Headings */}
      <button onClick={() => editor.chain().focus().toggleHeading({ level: 1 }).run()} className={btn(editor.isActive('heading', { level: 1 }))}>
        H1
      </button>
      <button onClick={() => editor.chain().focus().toggleHeading({ level: 2 }).run()} className={btn(editor.isActive('heading', { level: 2 }))}>
        H2
      </button>
      <button onClick={() => editor.chain().focus().setParagraph().run()} className={btn(editor.isActive('paragraph'))}>
        P
      </button>

      <div className="w-px h-5 bg-ink/10 mx-1" />

      {/* Alignment */}
      <button onClick={() => editor.chain().focus().setTextAlign('left').run()} className={btn(editor.isActive({ textAlign: 'left' }))}>
        ←
      </button>
      <button onClick={() => editor.chain().focus().setTextAlign('center').run()} className={btn(editor.isActive({ textAlign: 'center' }))}>
        ↔
      </button>
      <button onClick={() => editor.chain().focus().setTextAlign('right').run()} className={btn(editor.isActive({ textAlign: 'right' }))}>
        →
      </button>

      <div className="w-px h-5 bg-ink/10 mx-1" />

      {/* Lists */}
      <button onClick={() => editor.chain().focus().toggleBulletList().run()} className={btn(editor.isActive('bulletList'))}>
        • List
      </button>
      <button onClick={() => editor.chain().focus().toggleOrderedList().run()} className={btn(editor.isActive('orderedList'))}>
        1. List
      </button>

      <div className="w-px h-5 bg-ink/10 mx-1" />

      {/* Color */}
      <label className="flex items-center gap-1 cursor-pointer px-2 py-1.5 rounded hover:bg-fog text-xs text-ink/70">
        Color
        <input
          type="color"
          className="w-4 h-4 rounded cursor-pointer border-0 p-0"
          onChange={e => editor.chain().focus().setColor(e.target.value).run()}
        />
      </label>

      <div className="w-px h-5 bg-ink/10 mx-1" />

      {/* Image + Link */}
      <button onClick={addImage} className={btn(false)}>
        Image
      </button>
      <button onClick={setLink} className={btn(editor.isActive('link'))}>
        Link
      </button>

      <div className="w-px h-5 bg-ink/10 mx-1" />

      {/* Blockquote + Code */}
      <button onClick={() => editor.chain().focus().toggleBlockquote().run()} className={btn(editor.isActive('blockquote'))}>
        Quote
      </button>
      <button onClick={() => editor.chain().focus().toggleCode().run()} className={btn(editor.isActive('code'))}>
        Code
      </button>

      <div className="w-px h-5 bg-ink/10 mx-1" />

      <button onClick={() => editor.chain().focus().undo().run()} className={btn(false)}>Undo</button>
      <button onClick={() => editor.chain().focus().redo().run()} className={btn(false)}>Redo</button>
    </div>
  )
}

export default function TemplateEditorPage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()

  const [form, setForm] = useState({ name: '', subject: '', body: '' })
  const [isLoading, setIsLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [saved, setSaved] = useState(false)
  const [editorMode, setEditorMode] = useState<EditorMode>('html')
  const [showDelete, setShowDelete] = useState(false)

  const richEditor = useEditor({
    extensions: [
      StarterKit,
      TextStyle,
      Color,
      Underline,
      Image,
      Link.configure({ openOnClick: false }),
      TextAlign.configure({ types: ['heading', 'paragraph'] }),
    ],
    content: '',
    onUpdate: ({ editor }) => {
      setForm(f => ({ ...f, body: editor.getHTML() }))
    },
  })

  useEffect(() => {
    if (!id) return
    apiClient.get<{ id: string; name: string; subject: string; body: string }>(
      `/api/v1/templates/${id}`
    )
      .then(res => {
        setForm({ name: res.name, subject: res.subject, body: res.body })
        richEditor?.commands.setContent(res.body)
      })
      .catch(() => setError('Template not found.'))
      .finally(() => setIsLoading(false))
  }, [id, richEditor])

  async function handleSave() {
    setSaving(true)
    setError(null)
    setSaved(false)
    try {
      const body = editorMode === 'rich' ? richEditor?.getHTML() ?? form.body : form.body
      await apiClient.put(`/api/v1/templates/${id}`, { ...form, body })
      setSaved(true)
      setTimeout(() => setSaved(false), 3000)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to save.')
    } finally {
      setSaving(false)
    }
  }

  async function handleDelete() {
    try {
      await apiClient.delete(`/api/v1/templates/${id}`)
      navigate('/templates')
    } catch {
      setError('Failed to delete.')
    }
  }

  const placeholders = ['{{FirstName}}', '{{AppName}}', '{{CtaUrl}}', '{{BannerUrl}}', '{{UnsubscribeUrl}}']

  function insertPlaceholder(v: string) {
    if (editorMode === 'rich') {
      richEditor?.chain().focus().insertContent(v).run()
    } else {
      setForm(f => ({ ...f, body: f.body + v }))
    }
  }

  function switchMode(mode: EditorMode) {
    if (mode === 'rich' && editorMode === 'html') {
      richEditor?.commands.setContent(form.body)
    }
    if (mode === 'html' && editorMode === 'rich') {
      setForm(f => ({ ...f, body: richEditor?.getHTML() ?? f.body }))
    }
    setEditorMode(mode)
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
      {showDelete && (
        <DeleteModal
          name={form.name}
          onConfirm={handleDelete}
          onCancel={() => setShowDelete(false)}
        />
      )}

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
          {/* Mode toggle */}
          <div className="flex items-center bg-fog rounded-lg p-1 gap-1">
            <button
              onClick={() => switchMode('html')}
              className={`px-3 py-1.5 rounded-md text-xs font-medium transition ${
                editorMode === 'html' ? 'bg-white shadow-sm text-ink' : 'text-ink/50 hover:text-ink'
              }`}
            >
              HTML
            </button>
            <button
              onClick={() => switchMode('rich')}
              className={`px-3 py-1.5 rounded-md text-xs font-medium transition ${
                editorMode === 'rich' ? 'bg-white shadow-sm text-ink' : 'text-ink/50 hover:text-ink'
              }`}
            >
              Rich Text
            </button>
          </div>

          {saved && <span className="text-xs text-teal font-medium">Saved</span>}
          {error && <span className="text-xs text-coral">{error}</span>}

          <button
            onClick={() => setShowDelete(true)}
            className="px-4 py-2.5 border border-coral/30 text-coral rounded-lg text-sm font-medium hover:bg-red-50 transition"
          >
            Delete
          </button>
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
            value={form.subject}
            onChange={e => setForm(f => ({ ...f, subject: e.target.value }))}
          />
        </div>
      </div>

      {/* Placeholders */}
      <div className="flex flex-wrap gap-2 mb-4">
        {placeholders.map(v => (
          <button
            key={v}
            onClick={() => insertPlaceholder(v)}
            className="text-xs px-2 py-1 bg-violet/10 text-violet rounded font-mono hover:bg-violet/20 transition"
          >
            {v}
          </button>
        ))}
        <span className="text-xs text-ink/30 self-center ml-1">click to insert variable</span>
      </div>

      {/* Editor */}
      {editorMode === 'html' ? (
        <div className="grid lg:grid-cols-2 gap-4">
          <div className="flex flex-col">
            <span className="text-xs font-medium text-ink/50 uppercase tracking-wide mb-2">HTML Editor</span>
            <textarea
              className="flex-1 border border-ink/20 rounded-xl px-4 py-3 text-xs font-mono leading-relaxed bg-ink text-paper resize-none focus:outline-none focus:ring-1 focus:ring-violet"
              style={{ minHeight: '600px' }}
              spellCheck={false}
              value={form.body}
              onChange={e => setForm(f => ({ ...f, body: e.target.value }))}
            />
          </div>
          <div className="flex flex-col">
            <span className="text-xs font-medium text-ink/50 uppercase tracking-wide mb-2">Live Preview</span>
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
      ) : (
        <div className="border border-ink/10 rounded-xl overflow-hidden">
          <RichToolbar editor={richEditor} />
          <EditorContent
            editor={richEditor}
            className="min-h-[500px] px-6 py-4 prose prose-sm max-w-none focus:outline-none"
          />
        </div>
      )}
    </AppLayout>
  )
}