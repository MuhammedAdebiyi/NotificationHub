import { useState, useEffect } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import AppLayout from '@/app/layouts/AppLayout'
import { apiClient } from '@/shared/services/apiClient'
import { authService } from '@/shared/services/auth.service'

interface MemberProfile {
  id: string
  role: string
  joinedAt: string
  invitedAt: string | null
  revokedAt: string | null
  isRevoked: boolean
  user: {
    id: string
    fullName: string
    email: string
    isEmailVerified: boolean
    createdAt: string
  }
  activity: {
    notificationsSent: number
    templatesCreated: number
  }
}

export default function TeamMemberPage() {
  const { memberId } = useParams<{ memberId: string }>()
  const navigate = useNavigate()
  const currentUser = authService.getUser()
  const currentRole = currentUser?.role ?? 'member'
  const canManage = currentRole === 'owner' || currentRole === 'admin'

  const [member, setMember] = useState<MemberProfile | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [actionLoading, setActionLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    if (!memberId) return
    apiClient.get<MemberProfile>(`/api/v1/org/members/${memberId}`)
      .then(setMember)
      .catch(() => setError('Member not found.'))
      .finally(() => setIsLoading(false))
  }, [memberId])

  async function handleRevoke() {
    if (!confirm("Revoke this member's access? They can still log in but won't be able to use the organization.")) return
    setActionLoading(true)
    setError(null)
    try {
      await apiClient.put(`/api/v1/org/members/${memberId}/revoke`)
      const updated = await apiClient.get<MemberProfile>(`/api/v1/org/members/${memberId}`)
      setMember(updated)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to revoke access.')
    } finally {
      setActionLoading(false)
    }
  }

  async function handleRestore() {
    setActionLoading(true)
    setError(null)
    try {
      await apiClient.put(`/api/v1/org/members/${memberId}/restore`)
      const updated = await apiClient.get<MemberProfile>(`/api/v1/org/members/${memberId}`)
      setMember(updated)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to restore access.')
    } finally {
      setActionLoading(false)
    }
  }

  async function handleRoleChange(newRole: string) {
    setActionLoading(true)
    setError(null)
    try {
      await apiClient.put(`/api/v1/org/members/${memberId}/role`, { role: newRole })
      const updated = await apiClient.get<MemberProfile>(`/api/v1/org/members/${memberId}`)
      setMember(updated)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to update role.')
    } finally {
      setActionLoading(false)
    }
  }

  async function handleDelete() {
    if (!member) return
    if (!confirm(
      `Permanently remove ${member.user.fullName} from this organization? This cannot be undone — they'll need a new invite to rejoin.`
    )) return
    setActionLoading(true)
    setError(null)
    try {
      await apiClient.delete(`/api/v1/org/members/${memberId}`)
      navigate('/users')
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to remove member.')
      setActionLoading(false)
    }
  }

  const roleStyle: Record<string, string> = {
    owner: 'bg-violet/10 text-violet',
    admin: 'bg-teal/10 text-teal',
    member: 'bg-fog text-ink/60',
    revoked: 'bg-red-100 text-red-600',
  }

  if (isLoading) return (
    <AppLayout>
      <p className="text-ink/40 text-sm">Loading...</p>
    </AppLayout>
  )

  if (error || !member) return (
    <AppLayout>
      <div className="text-center py-16">
        <p className="text-ink/60">{error ?? 'Member not found.'}</p>
        <button onClick={() => navigate('/users')} className="mt-4 text-violet text-sm hover:underline">
          Back to team
        </button>
      </div>
    </AppLayout>
  )

  return (
    <AppLayout>
      <div className="mb-6">
        <button
          onClick={() => navigate('/users')}
          className="text-sm text-ink/50 hover:text-ink transition mb-4 flex items-center gap-1"
        >
          ← Back to team
        </button>
        <p className="hand text-xl text-violet mb-1">member profile —</p>
        <h1 className="font-display font-bold text-3xl">{member.user.fullName}</h1>
      </div>

      {error && (
        <div className="bg-coral/10 text-coral text-sm px-4 py-3 rounded-xl mb-4">{error}</div>
      )}

      <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
        {/* Profile card */}
        <div className="md:col-span-2 space-y-6">
          <div className="bg-white border border-ink/10 rounded-xl p-6 space-y-4">
            <div className="flex items-start justify-between">
              <div>
                <p className="font-display font-bold text-xl">{member.user.fullName}</p>
                <p className="text-ink/60 text-sm">{member.user.email}</p>
              </div>
              <span className={`text-xs px-2 py-1 rounded-full font-medium ${roleStyle[member.role] ?? 'bg-fog text-ink/60'}`}>
                {member.role}
              </span>
            </div>

            <div className="border-t border-ink/5 pt-4 grid grid-cols-2 gap-4 text-sm">
              <div>
                <p className="text-ink/40 text-xs uppercase tracking-wide mb-1">Member ID</p>
                <p className="font-mono text-xs text-ink/60 break-all">{member.id}</p>
              </div>
              <div>
                <p className="text-ink/40 text-xs uppercase tracking-wide mb-1">User ID</p>
                <p className="font-mono text-xs text-ink/60 break-all">{member.user.id}</p>
              </div>
              <div>
                <p className="text-ink/40 text-xs uppercase tracking-wide mb-1">Joined</p>
                <p>{member.joinedAt ? new Date(member.joinedAt).toLocaleDateString() : '—'}</p>
              </div>
              <div>
                <p className="text-ink/40 text-xs uppercase tracking-wide mb-1">Invited</p>
                <p>{member.invitedAt ? new Date(member.invitedAt).toLocaleDateString() : '—'}</p>
              </div>
              <div>
                <p className="text-ink/40 text-xs uppercase tracking-wide mb-1">Email verified</p>
                <p>{member.user.isEmailVerified ? '✓ Verified' : '✗ Unverified'}</p>
              </div>
              {member.revokedAt && (
                <div>
                  <p className="text-ink/40 text-xs uppercase tracking-wide mb-1">Revoked</p>
                  <p className="text-red-500">{new Date(member.revokedAt).toLocaleDateString()}</p>
                </div>
              )}
            </div>
          </div>

          {/* Activity stats */}
          <div className="bg-white border border-ink/10 rounded-xl p-6">
            <p className="font-display font-bold text-sm uppercase tracking-wide text-ink/40 mb-4">
              Organization activity
            </p>
            <div className="grid grid-cols-2 gap-4">
              <div className="bg-fog/50 rounded-xl p-4 text-center">
                <p className="font-display font-bold text-3xl text-violet">
                  {member.activity.notificationsSent}
                </p>
                <p className="text-xs text-ink/50 mt-1">Notifications sent</p>
              </div>
              <div className="bg-fog/50 rounded-xl p-4 text-center">
                <p className="font-display font-bold text-3xl text-teal">
                  {member.activity.templatesCreated}
                </p>
                <p className="text-xs text-ink/50 mt-1">Templates created</p>
              </div>
            </div>
          </div>
        </div>

        {/* Actions card — only for owner/admin, and not on owner members */}
        {canManage && member.role !== 'owner' && (
          <div className="bg-white border border-ink/10 rounded-xl p-6 space-y-4 h-fit">
            <p className="font-display font-bold text-sm uppercase tracking-wide text-ink/40">Actions</p>

            <div>
              <p className="text-xs text-ink/50 mb-2">Change role</p>
              <div className="flex gap-2">
                <button
                  onClick={() => handleRoleChange('admin')}
                  disabled={actionLoading || member.role === 'admin' || member.isRevoked}
                  className="flex-1 text-xs py-2 border border-teal/30 text-teal rounded-lg hover:bg-teal/5 transition disabled:opacity-40"
                >
                  Make admin
                </button>
                <button
                  onClick={() => handleRoleChange('member')}
                  disabled={actionLoading || member.role === 'member' || member.isRevoked}
                  className="flex-1 text-xs py-2 border border-ink/20 text-ink/60 rounded-lg hover:bg-fog transition disabled:opacity-40"
                >
                  Make member
                </button>
              </div>
            </div>

            <div className="border-t border-ink/5 pt-4">
              {member.isRevoked ? (
                <button
                  onClick={handleRestore}
                  disabled={actionLoading}
                  className="w-full text-sm py-2.5 border border-teal/30 text-teal rounded-lg hover:bg-teal/5 transition disabled:opacity-50"
                >
                  {actionLoading ? 'Restoring...' : 'Restore access'}
                </button>
              ) : (
                <button
                  onClick={handleRevoke}
                  disabled={actionLoading}
                  className="w-full text-sm py-2.5 border border-red-200 text-red-500 rounded-lg hover:bg-red-50 transition disabled:opacity-50"
                >
                  {actionLoading ? 'Revoking...' : 'Revoke access'}
                </button>
              )}
              <p className="text-xs text-ink/40 mt-2 text-center">
                {member.isRevoked
                  ? 'Restoring gives them back member access.'
                  : "They can still log in but won't be able to use this org."}
              </p>
            </div>

            <div className="border-t border-ink/5 pt-4">
              <button
                onClick={handleDelete}
                disabled={actionLoading}
                className="w-full text-sm py-2.5 bg-red-500 text-white rounded-lg hover:bg-red-600 transition disabled:opacity-50"
              >
                {actionLoading ? 'Removing...' : 'Remove from organization'}
              </button>
              <p className="text-xs text-ink/40 mt-2 text-center">
                This permanently deletes their membership. They'll need a new invite to rejoin.
              </p>
            </div>
          </div>
        )}
      </div>
    </AppLayout>
  )
}