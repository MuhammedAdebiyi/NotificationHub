import { useState, useEffect, useCallback } from 'react'
import { useNavigate } from 'react-router-dom'
import AppLayout from '@/app/layouts/AppLayout'
import { apiClient } from '@/shared/services/apiClient'
import { authService } from '@/shared/services/auth.service'

interface Member {
  id: string
  role: string
  joinedAt: string
  revokedAt: string | null
  isRevoked: boolean
  user: {
    id: string
    fullName: string
    email: string
    isEmailVerified: boolean
  }
}

interface Invite {
  id: string
  email: string
  role: string
  expiresAt: string
  createdAt: string
}

export default function UsersPage() {
  const navigate = useNavigate()
  const currentUser = authService.getUser()
  const currentRole = currentUser?.role ?? 'member'
  const canManage = currentRole === 'owner' || currentRole === 'admin'

  const [members, setMembers] = useState<Member[]>([])
  const [invites, setInvites] = useState<Invite[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [showInviteModal, setShowInviteModal] = useState(false)
  const [inviteEmail, setInviteEmail] = useState('')
  const [inviteRole, setInviteRole] = useState('member')
  const [inviteLoading, setInviteLoading] = useState(false)
  const [inviteError, setInviteError] = useState<string | null>(null)
  const [inviteSuccess, setInviteSuccess] = useState(false)

  const load = useCallback(async () => {
    try {
      const [membersRes, invitesRes] = await Promise.all([
        apiClient.get<Member[]>('/api/v1/org/members'),
        canManage
          ? apiClient.get<Invite[]>('/api/v1/org/invites')
          : Promise.resolve([]),
      ])
      setMembers(membersRes)
      setInvites(invitesRes as Invite[])
    } catch {
      // fail silently
    } finally {
      setIsLoading(false)
    }
  }, [canManage])

  useEffect(() => { load() }, [load])

  async function handleInvite(e: React.FormEvent) {
    e.preventDefault()
    setInviteLoading(true)
    setInviteError(null)
    try {
      await apiClient.post('/api/v1/org/invites', { email: inviteEmail, role: inviteRole })
      setInviteSuccess(true)
      setInviteEmail('')
      await load()
      setTimeout(() => {
        setShowInviteModal(false)
        setInviteSuccess(false)
      }, 2000)
    } catch (err) {
      setInviteError(err instanceof Error ? err.message : 'Failed to send invite.')
    } finally {
      setInviteLoading(false)
    }
  }

  async function handleCancelInvite(e: React.MouseEvent, id: string) {
    e.stopPropagation()
    try {
      await apiClient.delete(`/api/v1/org/invites/${id}`)
      await load()
    } catch {
      // fail silently
    }
  }

  const roleStyle: Record<string, string> = {
    owner: 'bg-violet/10 text-violet',
    admin: 'bg-teal/10 text-teal',
    member: 'bg-fog text-ink/60',
    revoked: 'bg-red-100 text-red-500',
  }

  const isEmpty = members.length === 0 && invites.length === 0

  return (
    <AppLayout>
      <div className="mb-8 flex items-center justify-between">
        <div>
          <p className="hand text-xl text-violet mb-1">your team —</p>
          <h1 className="font-display font-bold text-3xl">Team</h1>
        </div>
        {/* Only owner/admin see the invite button */}
        {canManage && (
          <button
            onClick={() => setShowInviteModal(true)}
            className="px-4 py-2 bg-ink text-white rounded-lg text-sm font-medium hover:bg-violet transition"
          >
            + Invite Member
          </button>
        )}
      </div>

      {isLoading ? (
        <p className="text-sm text-ink/40">Loading...</p>
      ) : isEmpty ? (
        <div className="text-center py-16 text-ink/40">
          <p className="text-lg font-medium mb-1">Just you for now</p>
          <p className="text-sm">Invite teammates to collaborate.</p>
        </div>
      ) : (
        <div className="bg-white border border-ink/10 rounded-xl overflow-hidden">
          <table className="w-full text-sm">
            <thead className="bg-fog/40 border-b border-ink/10">
              <tr>
                <th className="text-left px-4 py-3 font-medium">Name</th>
                <th className="text-left px-4 py-3 font-medium">Email</th>
                <th className="text-left px-4 py-3 font-medium">Role</th>
                <th className="text-left px-4 py-3 font-medium">Status</th>
                <th className="text-left px-4 py-3 font-medium">Joined</th>
                <th className="text-left px-4 py-3 font-medium"></th>
              </tr>
            </thead>
            <tbody className="divide-y divide-ink/5">
              {members.map(m => (
                <tr
                  key={m.id}
                  onClick={() => navigate(`/team/${m.id}`)}
                  className="hover:bg-fog/20 transition cursor-pointer"
                >
                  <td className="px-4 py-3 font-medium">{m.user.fullName}</td>
                  <td className="px-4 py-3 text-ink/60">{m.user.email}</td>
                  <td className="px-4 py-3">
                    <span className={`text-xs px-2 py-0.5 rounded-full font-medium ${roleStyle[m.role] ?? 'bg-fog text-ink/60'}`}>
                      {m.role}
                    </span>
                  </td>
                  <td className="px-4 py-3">
                    <span className={`text-xs px-2 py-0.5 rounded-full font-medium ${
                      m.role === 'revoked' ? 'bg-red-100 text-red-500' : 'bg-teal/10 text-teal'
                    }`}>
                      {m.role === 'revoked' ? 'Revoked' : 'Active'}
                    </span>
                  </td>
                  <td className="px-4 py-3 text-ink/40">
                    {m.joinedAt ? new Date(m.joinedAt).toLocaleDateString() : '—'}
                  </td>
                  <td className="px-4 py-3 text-ink/40 text-xs">View →</td>
                </tr>
              ))}

              {/* Pending invites — only visible to owner/admin */}
              {canManage && invites.map(i => (
                <tr key={i.id} className="opacity-60">
                  <td className="px-4 py-3 text-ink/40 italic">Invited</td>
                  <td className="px-4 py-3 text-ink/60">{i.email}</td>
                  <td className="px-4 py-3">
                    <span className={`text-xs px-2 py-0.5 rounded-full font-medium ${roleStyle[i.role] ?? 'bg-fog text-ink/60'}`}>
                      {i.role}
                    </span>
                  </td>
                  <td className="px-4 py-3">
                    <span className="text-xs px-2 py-0.5 rounded-full bg-yellow-100 text-yellow-700 font-medium">
                      Pending
                    </span>
                  </td>
                  <td className="px-4 py-3 text-ink/40">—</td>
                  <td className="px-4 py-3">
                    <button
                      onClick={(e) => handleCancelInvite(e, i.id)}
                      className="text-xs text-red-400 hover:text-red-600 transition"
                    >
                      Cancel
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {/* Invite modal — only rendered for owner/admin */}
      {canManage && showInviteModal && (
        <div className="fixed inset-0 bg-ink/40 backdrop-blur-sm flex items-center justify-center z-50 px-4">
          <div className="bg-paper rounded-2xl shadow-xl p-8 w-full max-w-md">
            <div className="flex items-center justify-between mb-6">
              <h2 className="font-display font-bold text-xl">Invite a teammate</h2>
              <button
                onClick={() => { setShowInviteModal(false); setInviteError(null); setInviteSuccess(false) }}
                className="text-ink/40 hover:text-ink text-xl"
              >
                ✕
              </button>
            </div>

            {inviteSuccess ? (
              <div className="text-center py-6">
                <div className="w-12 h-12 rounded-full bg-teal/20 flex items-center justify-center mx-auto mb-3">
                  <span className="text-xl text-teal">✓</span>
                </div>
                <p className="font-medium">Invite sent!</p>
                <p className="text-sm text-ink/60 mt-1">They'll receive an email shortly.</p>
              </div>
            ) : (
              <form onSubmit={handleInvite} className="space-y-4">
                {inviteError && (
                  <div className="bg-coral/10 text-coral text-sm px-4 py-3 rounded-xl">
                    {inviteError}
                  </div>
                )}
                <input
                  type="email"
                  placeholder="teammate@company.com"
                  value={inviteEmail}
                  onChange={(e) => setInviteEmail(e.target.value)}
                  required
                  className="w-full bg-fog border border-ink/10 rounded-xl px-4 py-3 text-sm focus:outline-none focus:ring-2 focus:ring-violet"
                />
                <select
                  value={inviteRole}
                  onChange={(e) => setInviteRole(e.target.value)}
                  className="w-full bg-fog border border-ink/10 rounded-xl px-4 py-3 text-sm focus:outline-none focus:ring-2 focus:ring-violet"
                >
                  <option value="member">Member</option>
                  <option value="admin">Admin</option>
                </select>
                <button
                  type="submit"
                  disabled={inviteLoading}
                  className="w-full bg-ink text-white font-semibold text-sm py-3.5 rounded-xl hover:bg-violet transition disabled:opacity-50"
                >
                  {inviteLoading ? 'Sending...' : 'Send invite'}
                </button>
              </form>
            )}
          </div>
        </div>
      )}
    </AppLayout>
  )
}