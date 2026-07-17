import { useEffect, useState } from 'react'
import { useSearchParams, useNavigate } from 'react-router-dom'
import { apiClient } from '@/shared/services/apiClient'
import { authService } from '@/shared/services/auth.service'
import AuthLayout from '@/app/layouts/AuthLayout'

interface InviteInfo {
  email: string
  role: string
  organizationName: string
  expiresAt: string
}

interface AcceptResponse {
  token: string
  userId: string
  organizationId: string
  email: string
}

export default function AcceptInvitePage() {
  const [searchParams] = useSearchParams()
  const navigate = useNavigate()
  const token = searchParams.get('token') ?? ''

  const [inviteInfo, setInviteInfo] = useState<InviteInfo | null>(null)
  const [status, setStatus] = useState<'loading' | 'ready' | 'submitting' | 'error'>('loading')
  const [errorMessage, setErrorMessage] = useState('')
  const [form, setForm] = useState({ fullName: '', password: '', confirmPassword: '' })
  const [isExistingUser, setIsExistingUser] = useState(false)

  useEffect(() => {
    if (!token) {
      setStatus('error')
      setErrorMessage('No invite token found.')
      return
    }

    apiClient
    .get<InviteInfo>(`/api/v1/org/invites/validate?token=${encodeURIComponent(token)}`)
    .then((info) => {
      setInviteInfo(info)

      const loggedInUser = authService.getUser()
      const matchesInvite =
        loggedInUser?.email?.toLowerCase() === info.email.toLowerCase()

      setIsExistingUser(matchesInvite)
      setStatus('ready')
    })
    .catch((err: Error) => {
      setStatus('error')
      setErrorMessage(err.message || 'This invite is invalid or has expired.')
    })
    }, [token])

  async function handleAccept(e: React.FormEvent) {
    e.preventDefault()
    setStatus('submitting')

    try {
      const response = await apiClient.post<AcceptResponse>(
        '/api/v1/org/invites/accept',
        isExistingUser
          ? { token }
          : { token, fullName: form.fullName, password: form.password }
      )
      authService.setToken(response.token)
      navigate('/dashboard')
    } catch (err: unknown) {
      setStatus('ready')
      setErrorMessage(err instanceof Error ? err.message : 'Failed to accept invite.')
    }
  }

  function handleChange(e: React.ChangeEvent<HTMLInputElement>) {
    setForm({ ...form, [e.target.name]: e.target.value })
  }

  if (status === 'loading') {
    return (
      <AuthLayout>
        <div className="max-w-md mx-auto py-24 text-center">
          <div className="w-10 h-10 border-4 border-violet border-t-transparent rounded-full animate-spin mx-auto mb-4" />
          <p className="text-ink/60">Validating invite...</p>
        </div>
      </AuthLayout>
    )
  }

  if (status === 'error') {
    return (
      <AuthLayout>
        <div className="max-w-md mx-auto py-24 text-center">
          <div className="w-14 h-14 rounded-full bg-coral/20 flex items-center justify-center mx-auto mb-4">
            <span className="text-2xl text-coral">✕</span>
          </div>
          <h1 className="font-display font-bold text-2xl mb-3">Invite invalid</h1>
          <p className="text-ink/60 mb-6">{errorMessage}</p>
          <a href="/login" className="text-violet font-medium hover:underline text-sm">
            Go to login
          </a>
        </div>
      </AuthLayout>
    )
  }

  return (
    <AuthLayout>
      <div className="max-w-md mx-auto py-12">
        <p className="hand text-xl text-violet mb-1">you've been invited —</p>
        <h1 className="font-display font-bold text-3xl mb-2">
          Join {inviteInfo?.organizationName}
        </h1>
        <p className="text-ink/60 mb-8">
          as <strong>{inviteInfo?.role}</strong> · {inviteInfo?.email}
        </p>

        {errorMessage && (
          <div className="bg-coral/10 text-coral text-sm px-4 py-3 rounded-xl mb-4">
            {errorMessage}
          </div>
        )}

        <form onSubmit={handleAccept} className="space-y-4">
          {!isExistingUser && (
            <>
              <input
                name="fullName"
                placeholder="Your full name"
                value={form.fullName}
                onChange={handleChange}
                required
                className="w-full bg-fog border border-ink/10 rounded-xl px-4 py-3 text-sm focus:outline-none focus:ring-2 focus:ring-violet"
              />
              <input
                type="password"
                name="password"
                placeholder="Create a password"
                value={form.password}
                onChange={handleChange}
                required
                className="w-full bg-fog border border-ink/10 rounded-xl px-4 py-3 text-sm focus:outline-none focus:ring-2 focus:ring-violet"
              />
            </>
          )}

          <button
            type="submit"
            disabled={status === 'submitting'}
            className="w-full bg-ink text-white font-semibold text-sm py-3.5 rounded-xl hover:bg-violet transition disabled:opacity-50"
          >
            {status === 'submitting'
              ? 'Joining...'
              : isExistingUser
              ? `Join ${inviteInfo?.organizationName}`
              : 'Create account & join'}
          </button>
        </form>

        {isExistingUser && (
          <p className="text-sm text-ink/50 mt-4 text-center">
            Joining as your existing account ({inviteInfo?.email})
          </p>
        )}
      </div>
    </AuthLayout>
  )
}