import { useLocation, useNavigate } from 'react-router-dom'
import { useState } from 'react'
import { apiClient } from '@/shared/services/apiClient'
import { authService } from '@/shared/services/auth.service'
import { ROLES } from '@/shared/utils/roles'
import type { OrgOption } from '@/features/auth/types/auth.types'

interface LocationState {
  userId: string
  email: string
  fullName: string
  organizations: OrgOption[]
}

const roleStyle: Record<string, string> = {
  [ROLES.OWNER]: 'bg-violet/10 text-violet',
  [ROLES.ADMIN]: 'bg-teal/10 text-teal',
  [ROLES.MEMBER]: 'bg-fog border border-ink/10 text-ink/60',
  [ROLES.REVOKED]: 'bg-coral/10 text-coral',
}

export default function SelectOrgPage() {
  const navigate = useNavigate()
  const location = useLocation()
  const state = location.state as LocationState | null

  const [loading, setLoading] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)

  if (!state?.organizations?.length) {
    navigate('/login')
    return null
  }

  const firstName = state.fullName?.split(' ')[0] ?? 'there'

  async function handleSelect(org: OrgOption) {
    setLoading(org.organizationId)
    setError(null)
    try {
      const res = await apiClient.post<{ token: string }>(
        '/api/v1/auth/select-org',
        { userId: state!.userId, organizationId: org.organizationId }
      )
      authService.setToken(res.token)
      navigate('/dashboard')
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to select organization.')
      setLoading(null)
    }
  }

  return (
    <div className="min-h-screen bg-paper flex items-center justify-center px-4">
      <div className="w-full max-w-md">
        <p className="hand text-2xl text-violet mb-1">welcome back —</p>
        <h1 className="font-display font-bold text-3xl mb-2">
          Hi, {firstName}
        </h1>
        <p className="text-sm text-ink/50 mb-8">
          You're part of multiple organizations. Which one are you working in today?
        </p>

        {error && (
          <div className="bg-coral/10 text-coral text-sm px-4 py-3 rounded-xl mb-4">
            {error}
          </div>
        )}

        <div className="space-y-3">
          {state.organizations.map(org => (
            <button
              key={org.organizationId}
              onClick={() => handleSelect(org)}
              disabled={!!loading}
              className="w-full text-left bg-white border border-ink/10 rounded-xl px-5 py-4 hover:border-violet hover:shadow-sm transition disabled:opacity-50 group"
            >
              <div className="flex items-center justify-between">
                <div>
                  <p className="font-display font-semibold text-base group-hover:text-violet transition">
                    {org.orgName}
                  </p>
                  <span className={`text-xs px-2 py-0.5 rounded-full font-medium mt-1 inline-block ${roleStyle[org.role] ?? roleStyle.member}`}>
                    {org.role}
                  </span>
                </div>
                <span className="text-ink/30 group-hover:text-violet transition text-lg">
                  {loading === org.organizationId ? '...' : '→'}
                </span>
              </div>
            </button>
          ))}
        </div>

        <button
          onClick={() => { authService.clearToken(); navigate('/login') }}
          className="mt-8 text-sm text-ink/40 hover:text-ink transition"
        >
          ← Sign in with a different account
        </button>
      </div>
    </div>
  )
}