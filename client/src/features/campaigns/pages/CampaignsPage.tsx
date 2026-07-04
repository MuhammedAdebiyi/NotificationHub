import { useState } from 'react'
import AppLayout from '@/app/layouts/AppLayout'

interface Campaign {
  id: string
  title: string
  subject: string
  channel: string
  status: 'Draft' | 'Scheduled' | 'Sending' | 'Sent' | 'Paused'
  totalRecipients: number
  scheduledAt: string | null
  createdAt: string
}

const MOCK_CAMPAIGNS: Campaign[] = [
  {
    id: '1',
    title: 'July Product Update',
    subject: 'What\'s new in July — big things coming',
    channel: 'Email',
    status: 'Sent',
    totalRecipients: 1240,
    scheduledAt: '2026-07-01T09:00:00Z',
    createdAt: '2026-06-28T14:00:00Z',
  },
  {
    id: '2',
    title: 'Welcome Series — Week 1',
    subject: 'Getting started with {{AppName}}',
    channel: 'Email',
    status: 'Sending',
    totalRecipients: 380,
    scheduledAt: null,
    createdAt: '2026-07-03T10:00:00Z',
  },
  {
    id: '3',
    title: 'Re-engagement Blast',
    subject: 'We miss you — here\'s what you\'ve missed',
    channel: 'Email',
    status: 'Scheduled',
    totalRecipients: 892,
    scheduledAt: '2026-07-10T08:00:00Z',
    createdAt: '2026-07-04T09:00:00Z',
  },
  {
    id: '4',
    title: 'Feature Announcement Draft',
    subject: 'Introducing campaigns — send to everyone at once',
    channel: 'Email',
    status: 'Draft',
    totalRecipients: 0,
    scheduledAt: null,
    createdAt: '2026-07-04T13:00:00Z',
  },
]

const statusStyle: Record<Campaign['status'], string> = {
  Draft: 'bg-ink/10 text-ink/50',
  Scheduled: 'bg-violet/10 text-violet',
  Sending: 'bg-yellow-100 text-yellow-700',
  Sent: 'bg-teal/10 text-teal',
  Paused: 'bg-coral/10 text-coral',
}

export default function CampaignsPage() {
  const [campaigns] = useState<Campaign[]>(MOCK_CAMPAIGNS)
  const [showForm, setShowForm] = useState(false)
  const [form, setForm] = useState({ title: '', subject: '', channel: 'Email' })

  const sent = campaigns.filter(c => c.status === 'Sent').length
  const scheduled = campaigns.filter(c => c.status === 'Scheduled').length
  const totalReached = campaigns
    .filter(c => c.status === 'Sent')
    .reduce((acc, c) => acc + c.totalRecipients, 0)

  return (
    <AppLayout>
      <div className="mb-8 flex items-center justify-between">
        <div>
          <p className="hand text-xl text-violet mb-1">bulk sending —</p>
          <h1 className="font-display font-bold text-3xl">Campaigns</h1>
        </div>
        <button
          onClick={() => setShowForm(true)}
          className="px-4 py-2 bg-ink text-white rounded-lg text-sm font-medium hover:bg-ink/80 transition"
        >
          + New Campaign
        </button>
      </div>

      {/* Stats row */}
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

      {/* Create form */}
      {showForm && (
        <div className="bg-white border border-ink/10 rounded-xl p-6 mb-6">
          <h2 className="font-display font-bold text-lg mb-4">New Campaign</h2>
          <div className="space-y-4">
            <div>
              <label className="block text-sm font-medium mb-1">Campaign Name</label>
              <input
                className="w-full border border-ink/20 rounded-lg px-3 py-2 text-sm"
                placeholder="e.g. July Product Update"
                value={form.title}
                onChange={e => setForm(f => ({ ...f, title: e.target.value }))}
              />
            </div>
            <div>
              <label className="block text-sm font-medium mb-1">Subject Line</label>
              <input
                className="w-full border border-ink/20 rounded-lg px-3 py-2 text-sm"
                placeholder="e.g. What's new this month"
                value={form.subject}
                onChange={e => setForm(f => ({ ...f, subject: e.target.value }))}
              />
            </div>
            <div>
              <label className="block text-sm font-medium mb-1">Channel</label>
              <select
                className="w-full border border-ink/20 rounded-lg px-3 py-2 text-sm"
                value={form.channel}
                onChange={e => setForm(f => ({ ...f, channel: e.target.value }))}
              >
                <option>Email</option>
                <option disabled>SMS (coming soon)</option>
                <option disabled>Push (coming soon)</option>
              </select>
            </div>
            <div className="bg-violet/5 border border-violet/20 rounded-lg p-4">
              <p className="text-xs text-violet font-medium">
                ⚡ Campaign sending is coming in V2 — create drafts now, send when it ships.
              </p>
            </div>
            <div className="flex gap-3">
              <button
                onClick={() => setShowForm(false)}
                className="px-4 py-2 bg-ink text-white rounded-lg text-sm font-medium hover:bg-ink/80 transition"
              >
                Save as Draft
              </button>
              <button
                onClick={() => setShowForm(false)}
                className="px-4 py-2 border border-ink/20 rounded-lg text-sm font-medium hover:bg-fog transition"
              >
                Cancel
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Campaign list */}
      <div className="bg-fog border border-ink/10 rounded-xl overflow-hidden">
        <table className="w-full text-sm">
          <thead className="border-b border-ink/10">
            <tr className="text-left text-ink/50 text-xs uppercase tracking-wide">
              <th className="px-4 py-3 font-medium">Campaign</th>
              <th className="px-4 py-3 font-medium">Channel</th>
              <th className="px-4 py-3 font-medium">Status</th>
              <th className="px-4 py-3 font-medium">Recipients</th>
              <th className="px-4 py-3 font-medium">Scheduled</th>
              <th className="px-4 py-3 font-medium">Created</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-ink/5">
            {campaigns.map(c => (
              <tr key={c.id} className="hover:bg-fog/50 transition cursor-pointer">
                <td className="px-4 py-3">
                  <p className="font-medium">{c.title}</p>
                  <p className="text-xs text-ink/40 mt-0.5 truncate max-w-xs">{c.subject}</p>
                </td>
                <td className="px-4 py-3 text-ink/60">{c.channel}</td>
                <td className="px-4 py-3">
                  <span className={`text-xs px-2 py-0.5 rounded-full font-medium ${statusStyle[c.status]}`}>
                    {c.status}
                  </span>
                </td>
                <td className="px-4 py-3 text-ink/60">
                  {c.totalRecipients > 0 ? c.totalRecipients.toLocaleString() : '—'}
                </td>
                <td className="px-4 py-3 text-ink/40 text-xs">
                  {c.scheduledAt
                    ? new Date(c.scheduledAt).toLocaleDateString()
                    : '—'}
                </td>
                <td className="px-4 py-3 text-ink/40 text-xs">
                  {new Date(c.createdAt).toLocaleDateString()}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </AppLayout>
  )
}