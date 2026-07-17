import { useState, useEffect, useRef } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import AppLayout from '@/app/layouts/AppLayout'
import { campaignApi } from '../api/campaignApi'
import { apiClient } from '@/shared/services/apiClient'
import CampaignProgressPanel from "@/features/campaigns/components/CampaignProgressPanel"
import ImportRecipientsPanel from "@/features/campaigns/components/ImportRecipientsPanel"
import ImportProgressPanel from "@/features/campaigns/components/ImportProgressPanel"
import type { CampaignDetail, CampaignStatus } from '../types/campaign.types'
import type { ImportJob } from '../types/import.types'

interface CampaignNotification {
  publicId: string
  recipientEmail: string
  type: string
  channel: string
  status: string
  retryCount: number
  createdAt: string
}

const statusStyle: Record<CampaignStatus, string> = {
  Draft: 'bg-ink/10 text-ink/50',
  Scheduled: 'bg-violet/10 text-violet',
  Running: 'bg-yellow/20 text-ink',
  Sent: 'bg-teal/10 text-teal',
  Paused: 'bg-coral/10 text-coral',
  Completed: 'bg-teal/10 text-teal',
  Cancelled: 'bg-ink/10 text-ink/40',
}

type InputMode = 'paste' | 'csv' | 'database'

export default function CampaignDetailPage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const fileInputRef = useRef<HTMLInputElement>(null)

  const [campaign, setCampaign] = useState<CampaignDetail | null>(null)
  const campaignRef = useRef<CampaignDetail | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [inputMode, setInputMode] = useState<InputMode>('paste')
  const [recipientInput, setRecipientInput] = useState('')
  const [csvEmails, setCsvEmails] = useState<string[]>([])
  const [csvFileName, setCsvFileName] = useState('')
  const [addingRecipients, setAddingRecipients] = useState(false)
  const [addResult, setAddResult] = useState<{ added: number; skipped: number } | null>(null)
  const [actionError, setActionError] = useState<string | null>(null)
  const [scheduleMode, setScheduleMode] = useState(false)
  const [scheduledAt, setScheduledAt] = useState('')
  const [notifications, setNotifications] = useState<CampaignNotification[]>([])
  const [failedCount, setFailedCount] = useState(0)
  const [showDeleteModal, setShowDeleteModal] = useState(false)
  const [activeImportJobId, setActiveImportJobId] = useState<string | null>(null)

  async function load() {
    if (!id) return
    try {
      const [data, notifData] = await Promise.all([
        campaignApi.getById(id),
        apiClient.get<{ items: CampaignNotification[]; failedCount: number }>(
          `/api/v1/campaigns/${id}/notifications`
        ),
      ])
      setCampaign(data)
      campaignRef.current = data
      setNotifications(notifData.items)
      setFailedCount(notifData.failedCount)
    } catch {
      navigate('/campaigns')
    } finally {
      setIsLoading(false)
    }
  }

  useEffect(() => {
    load()

    const interval = setInterval(() => {
      if (campaignRef.current?.status?.toLowerCase() === 'running') {
        load()
      }
    }, 5000)

    return () => clearInterval(interval)
  }, [id])

  function handleCsvUpload(e: React.ChangeEvent<HTMLInputElement>) {
    const file = e.target.files?.[0]
    if (!file) return
    setCsvFileName(file.name)
    const reader = new FileReader()
    reader.onload = (ev) => {
      const text = ev.target?.result as string
      const emails = text
        .split(/[\n,;\r]+/)
        .map(s => s.trim().toLowerCase())
        .filter(s => s.includes('@') && s.includes('.'))
      setCsvEmails(emails)
    }
    reader.readAsText(file)
  }

  async function handleAddRecipients(e: React.FormEvent) {
    e.preventDefault()
    if (!id) return
    setAddingRecipients(true)
    setAddResult(null)
    setActionError(null)
    try {
      let emails: string[] = []
      if (inputMode === 'paste') {
        emails = recipientInput
          .split(/[\n,;\s]+/)
          .map(s => s.trim().toLowerCase())
          .filter(s => s.includes('@'))
      } else {
        emails = csvEmails
      }

      if (emails.length === 0) {
        setActionError('No valid email addresses found.')
        return
      }

      const result = await campaignApi.addRecipients(id, emails)
      setAddResult(result)
      setRecipientInput('')
      setCsvEmails([])
      setCsvFileName('')
      if (fileInputRef.current) fileInputRef.current.value = ''
      await load()
    } catch (err) {
      setActionError(err instanceof Error ? err.message : 'Failed to add recipients.')
    } finally {
      setAddingRecipients(false)
    }
  }

  function handleImportStarted(job: ImportJob) {
    setActiveImportJobId(job.id)
    setActionError(null)
  }

  async function handleImportComplete() {
    await load()
  }

  async function handleAction(action: 'send' | 'schedule' | 'pause' | 'resume' | 'delete') {
    if (!id) return
    setActionError(null)
    try {
      if (action === 'send') await campaignApi.send(id)
      else if (action === 'schedule') {
        if (!scheduledAt) { setActionError('Please pick a date and time.'); return }
        await campaignApi.schedule(id, new Date(scheduledAt).toISOString())
        setScheduleMode(false)
      }
      else if (action === 'pause') await campaignApi.pause(id)
      else if (action === 'resume') await campaignApi.resume(id)
      else if (action === 'delete') {
        await campaignApi.delete(id)
        navigate('/campaigns')
        return
      }
      await load()
    } catch (err) {
      setActionError(err instanceof Error ? err.message : 'Action failed.')
    }
  }

  if (isLoading) return (
    <AppLayout>
      <div className="text-sm text-ink/40 py-20 text-center">Loading...</div>
    </AppLayout>
  )

  if (!campaign) return null

  const isDraft = campaign.status?.toLowerCase() === 'draft'
  const isRunning = campaign.status?.toLowerCase() === 'running'
  const isPaused = campaign.status?.toLowerCase() === 'paused'
  const isEditable = isDraft || isPaused
  const canSend = isDraft && campaign.totalRecipients > 0

  return (
    <AppLayout>
      {/* Delete confirmation modal */}
      {showDeleteModal && (
        <div className="fixed inset-0 z-50 flex items-center justify-center">
          <div className="absolute inset-0 bg-ink/40 backdrop-blur-sm" onClick={() => setShowDeleteModal(false)} />
          <div className="relative bg-white rounded-2xl p-8 max-w-sm w-full mx-4 shadow-2xl">
            <div className="w-12 h-12 bg-coral/10 rounded-full flex items-center justify-center mb-4">
              <span className="text-coral text-xl">✕</span>
            </div>
            <h3 className="font-display font-bold text-lg mb-1">Delete campaign?</h3>
            <p className="text-sm text-ink/60 mb-6">
              <span className="font-medium text-ink">"{campaign.title}"</span> will be permanently deleted.
            </p>
            <div className="flex gap-3">
              <button
                onClick={async () => { setShowDeleteModal(false); await handleAction('delete') }}
                className="flex-1 px-4 py-2.5 bg-coral text-white rounded-lg text-sm font-medium hover:bg-coral/80 transition"
              >
                Delete
              </button>
              <button
                onClick={() => setShowDeleteModal(false)}
                className="flex-1 px-4 py-2.5 border border-ink/20 rounded-lg text-sm font-medium hover:bg-fog transition"
              >
                Cancel
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Header */}
      <div className="mb-6">
        <button
          onClick={() => navigate('/campaigns')}
          className="text-sm text-ink/50 hover:text-ink transition mb-4 inline-flex items-center gap-1"
        >
          ← Back to campaigns
        </button>

        <div className="flex items-start justify-between flex-wrap gap-4">
          <div>
            <p className="hand text-xl text-violet mb-1">campaign —</p>
            <h1 className="font-display font-bold text-3xl">{campaign.title}</h1>
            <p className="text-ink/50 mt-1">{campaign.subject}</p>
          </div>

          <div className="flex items-center gap-2 flex-wrap mt-2">
            <span className={`text-xs px-3 py-1 rounded-full font-medium ${statusStyle[campaign.status]}`}>
              {campaign.status}
            </span>
            {failedCount > 0 && (
              <span className="text-xs px-3 py-1 rounded-full font-medium bg-coral/10 text-coral">
                {failedCount} failed
              </span>
            )}

            {isDraft && (
              <>
                {canSend ? (
                  <button
                    onClick={() => handleAction('send')}
                    className="px-4 py-2 bg-teal text-white text-sm rounded-lg hover:bg-teal/80 transition"
                  >
                    Send now
                  </button>
                ) : (
                  <span className="text-xs text-ink/40 italic">Add recipients to send</span>
                )}
                <button
                  onClick={() => setScheduleMode(s => !s)}
                  className="px-4 py-2 border border-violet/30 text-violet text-sm rounded-lg hover:bg-violet/5 transition"
                >
                  Schedule
                </button>
              </>
            )}

            {isRunning && (
              <button
                onClick={() => handleAction('pause')}
                className="px-4 py-2 bg-yellow/80 text-ink text-sm rounded-lg hover:bg-yellow transition"
              >
                Pause
              </button>
            )}

            {isPaused && (
              <button
                onClick={() => handleAction('resume')}
                className="px-4 py-2 bg-violet text-white text-sm rounded-lg hover:bg-violet/80 transition"
              >
                Resume
              </button>
            )}

            {!isRunning && (
              <button
                onClick={() => setShowDeleteModal(true)}
                className="px-4 py-2 border border-coral/30 text-coral text-sm rounded-lg hover:bg-coral/10 transition"
              >
                Delete
              </button>
            )}
          </div>
        </div>

        {scheduleMode && (
          <div className="mt-4 flex items-center gap-3 bg-fog border border-ink/10 rounded-xl px-4 py-3">
            <input
              type="datetime-local"
              value={scheduledAt}
              onChange={e => setScheduledAt(e.target.value)}
              className="border border-ink/20 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-violet"
            />
            <button
              onClick={() => handleAction('schedule')}
              className="px-4 py-2 bg-violet text-white text-sm rounded-lg hover:bg-violet/80 transition"
            >
              Confirm schedule
            </button>
            <button
              onClick={() => setScheduleMode(false)}
              className="text-sm text-ink/40 hover:text-ink"
            >
              Cancel
            </button>
          </div>
        )}
      </div>

      {actionError && (
        <div className="bg-coral/10 text-coral text-sm px-4 py-3 rounded-xl mb-4">{actionError}</div>
      )}

      {/* Stats */}
      <div className="grid grid-cols-4 gap-4 mb-6">
        <div className="bg-fog border border-ink/10 rounded-xl p-5">
          <p className="font-display font-extrabold text-3xl text-teal">{campaign.stats.sent}</p>
          <p className="text-xs text-ink/50 mt-1">Sent</p>
        </div>
        <div className="bg-fog border border-ink/10 rounded-xl p-5">
          <p className="font-display font-extrabold text-3xl text-violet">{campaign.stats.pending}</p>
          <p className="text-xs text-ink/50 mt-1">Pending</p>
        </div>
        <div className={`border rounded-xl p-5 ${failedCount > 0 ? 'bg-coral/10 border-coral/20' : 'bg-fog border-ink/10'}`}>
          <p className={`font-display font-extrabold text-3xl ${failedCount > 0 ? 'text-coral' : 'text-ink/30'}`}>
            {failedCount}
          </p>
          <p className="text-xs text-ink/50 mt-1">Failed / DLQ</p>
        </div>
        <div className="bg-fog border border-ink/10 rounded-xl p-5">
          <p className="font-display font-extrabold text-3xl text-ink">{campaign.stats.total}</p>
          <p className="text-xs text-ink/50 mt-1">Total Recipients</p>
        </div>
      </div>

      {/* Message + Add Recipients */}
      <div className="grid lg:grid-cols-2 gap-6 mb-6">
        <div className="bg-fog border border-ink/10 rounded-xl p-6">
          <h2 className="font-display font-bold text-lg mb-3">Message</h2>
          <div
            className="text-sm text-ink/70 leading-relaxed prose max-w-none"
            dangerouslySetInnerHTML={{ __html: campaign.message }}
          />
        </div>

        {isEditable && (
          <div className="bg-fog border border-ink/10 rounded-xl p-6">
            <h2 className="font-display font-bold text-lg mb-1">Add Recipients</h2>
            <p className="text-xs text-ink/50 mb-4">
              {campaign.totalRecipients > 0
                ? `${campaign.totalRecipients} recipient${campaign.totalRecipients !== 1 ? 's' : ''} added so far`
                : 'No recipients yet — add some to send this campaign'}
            </p>

            <div className="flex gap-2 mb-4 flex-wrap">
              {(['paste', 'csv', 'database'] as InputMode[]).map(mode => (
                <button
                  key={mode}
                  onClick={() => setInputMode(mode)}
                  className={`text-xs px-3 py-1.5 rounded-lg font-medium transition ${
                    inputMode === mode
                      ? 'bg-ink text-white'
                      : 'border border-ink/20 text-ink/60 hover:bg-fog'
                  }`}
                >
                  {mode === 'paste' ? 'Paste emails' : mode === 'csv' ? 'CSV upload' : 'Import from Database'}
                </button>
              ))}
            </div>

            {addResult && (
              <div className="bg-teal/10 text-teal text-sm px-4 py-3 rounded-xl mb-3">
                ✓ Added {addResult.added} · Skipped {addResult.skipped} duplicates
              </div>
            )}

            {inputMode === 'database' ? (
              <>
                <ImportRecipientsPanel campaignId={campaign.id} onImportStarted={handleImportStarted} />
                {activeImportJobId && (
                  <ImportProgressPanel
                    campaignId={campaign.id}
                    importJobId={activeImportJobId}
                    onComplete={handleImportComplete}
                  />
                )}
              </>
            ) : (
              <form onSubmit={handleAddRecipients} className="space-y-3">
                {inputMode === 'paste' && (
                  <>
                    <textarea
                      className="w-full border border-ink/20 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-violet h-40 resize-none font-mono bg-white"
                      placeholder={`user@example.com\nanother@example.com`}
                      value={recipientInput}
                      onChange={e => setRecipientInput(e.target.value)}
                    />
                    <p className="text-xs text-ink/40">
                      Separate by comma, newline, or semicolon.
                      {recipientInput && ` ${recipientInput.split(/[\n,;\s]+/).filter(s => s.includes('@')).length} valid emails detected.`}
                    </p>
                  </>
                )}

                {inputMode === 'csv' && (
                  <div
                    onClick={() => fileInputRef.current?.click()}
                    className="w-full border-2 border-dashed border-ink/20 rounded-xl p-8 text-center cursor-pointer hover:border-violet/40 hover:bg-violet/5 transition"
                  >
                    <input
                      ref={fileInputRef}
                      type="file"
                      accept=".csv,.txt"
                      onChange={handleCsvUpload}
                      className="hidden"
                    />
                    {csvEmails.length > 0 ? (
                      <>
                        <p className="font-medium text-teal">{csvFileName}</p>
                        <p className="text-sm text-ink/50 mt-1">{csvEmails.length} valid emails found</p>
                      </>
                    ) : (
                      <>
                        <p className="text-ink/40 text-sm">Click to upload CSV or TXT file</p>
                        <p className="text-xs text-ink/30 mt-1">One email per line, or comma separated</p>
                      </>
                    )}
                  </div>
                )}

                <button
                  type="submit"
                  disabled={
                    addingRecipients ||
                    (inputMode === 'paste' && !recipientInput.trim()) ||
                    (inputMode === 'csv' && csvEmails.length === 0)
                  }
                  className="w-full bg-ink text-white text-sm font-medium py-2.5 rounded-lg hover:bg-violet transition disabled:opacity-50"
                >
                  {addingRecipients
                    ? 'Adding...'
                    : inputMode === 'csv' && csvEmails.length > 0
                    ? `Add ${csvEmails.length} recipients`
                    : 'Add Recipients'}
                </button>
              </form>
            )}
          </div>
        )}
      </div>

      {/* Notifications table */}
      {notifications.length > 0 && (
        <div className="mt-2">
          <div className="flex items-center gap-3 mb-3">
            <h2 className="font-display font-bold text-lg">Notifications</h2>
            {failedCount > 0 && (
              <span className="text-xs px-2 py-1 rounded-full bg-coral/10 text-coral font-medium">
                {failedCount} failed / DLQ
              </span>
            )}
          </div>
          <div className="bg-white border border-ink/10 rounded-xl overflow-hidden">
            <table className="w-full text-sm">
              <thead className="bg-fog/40 border-b border-ink/10">
                <tr>
                  <th className="text-left px-4 py-3 font-medium">Recipient</th>
                  <th className="text-left px-4 py-3 font-medium">Status</th>
                  <th className="text-left px-4 py-3 font-medium">Retries</th>
                  <th className="text-left px-4 py-3 font-medium">Sent</th>
                  <th className="text-left px-4 py-3 font-medium"></th>
                </tr>
              </thead>
              <tbody className="divide-y divide-ink/5">
                {notifications.map(n => (
                  <tr key={n.publicId} className="hover:bg-fog/20 transition">
                    <td className="px-4 py-3 font-mono text-xs text-ink/70">{n.recipientEmail}</td>
                    <td className="px-4 py-3">
                      <span className={`text-xs px-2 py-0.5 rounded-full font-medium ${
                        n.status === 'Sent' ? 'bg-teal/10 text-teal' :
                        n.status === 'DeadLetter' || n.status === 'Failed' ? 'bg-coral/10 text-coral' :
                        n.status === 'Processing' || n.status === 'Retrying' ? 'bg-yellow/20 text-ink' :
                        'bg-fog text-ink/50'
                      }`}>
                        {n.status}
                      </span>
                    </td>
                    <td className="px-4 py-3 text-ink/40">{n.retryCount}</td>
                    <td className="px-4 py-3 text-ink/40 text-xs">
                      {new Date(n.createdAt).toLocaleString()}
                    </td>
                    <td className="px-4 py-3">
                      {(n.status === 'DeadLetter' || n.status === 'Failed') && (
                      <a>
                        href={`/notifications/${n.publicId}`}
                        className="text-xs text-violet hover:underline"
                      
                        View & retry →
                      </a>
                    )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          {/* Duration banner — shown when completed */}
        {campaign.completedAt && campaign.startedAt && (
          <div className="bg-teal/10 border border-teal/20 rounded-xl px-5 py-4 mb-6">
            <p className="text-sm font-medium text-teal">
              ✓ Campaign completed in{' '}
              {(() => {
                const secs = Math.round(
                  (new Date(campaign.completedAt!).getTime() -
                  new Date(campaign.startedAt!).getTime()) / 1000
                )
                return secs < 60
                  ? `${secs} seconds`
                  : `${Math.floor(secs / 60)}m ${secs % 60}s`
              })()}
            </p>
            <p className="text-xs text-ink/50 mt-1">
              Started {new Date(campaign.startedAt).toLocaleString()} ·
              Finished {new Date(campaign.completedAt).toLocaleString()}
            </p>
          </div>
        )}
        {/* Live Progress Feed — shows when Running or just Completed */}
        {(isRunning || campaign.status?.toLowerCase() === 'completed') && (
          <CampaignProgressPanel campaignId={campaign.id} isRunning={isRunning} />
        )}
        </div> 
      )}
    </AppLayout>
  )
}