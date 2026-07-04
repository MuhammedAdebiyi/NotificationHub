import { useEffect, useState } from 'react'
import { useSearchParams, useNavigate } from 'react-router-dom'
import { apiClient } from '@/shared/services/apiClient'
import { authService } from '@/shared/services/auth.service'

type VerifyStatus = 'verifying' | 'success' | 'error'

interface VerifyResponse {
  token: string
  userId: string
  organizationId: string
  email: string
}

export default function VerifyEmail() {
  const [searchParams] = useSearchParams()
  const navigate = useNavigate()
  const token = searchParams.get('token')
  const [status, setStatus] = useState<VerifyStatus>('verifying')
  const [errorMessage, setErrorMessage] = useState<string>('')

  useEffect(() => {
    if (!token) {
      setStatus('error')
      setErrorMessage('No verification token found in the link.')
      return
    }

    apiClient
      .get<VerifyResponse>(`/api/v1/auth/verify-email?token=${encodeURIComponent(token)}`)
      .then((response) => {
        authService.setToken(response.token)
        setStatus('success')
        // Redirect to dashboard after short delay so user sees the success state
        setTimeout(() => navigate('/dashboard'), 1500)
      })
      .catch((err: Error) => {
        setStatus('error')
        setErrorMessage(err.message || 'Verification failed.')
      })
  }, [token])

  return (
    <div className="min-h-screen flex items-center justify-center bg-fog px-4">
      <div className="max-w-md w-full bg-paper rounded-2xl shadow-lg p-8 -rotate-1">
        {status === 'verifying' && (
          <div className="text-center">
            <div className="w-10 h-10 border-4 border-violet border-t-transparent rounded-full animate-spin mx-auto mb-4" />
            <h1 className="font-display text-xl text-ink mb-2">Verifying your email...</h1>
            <p className="text-ink/60">Hang tight, this only takes a second.</p>
          </div>
        )}

        {status === 'success' && (
          <div className="text-center">
            <div className="w-14 h-14 rounded-full bg-teal/20 flex items-center justify-center mx-auto mb-4">
              <span className="text-2xl text-teal">✓</span>
            </div>
            <h1 className="font-display text-xl text-ink mb-2">Email verified!</h1>
            <p className="text-ink/60">Taking you to your dashboard...</p>
          </div>
        )}

            {status === 'error' && (
        <div className="text-center">
          <div className="w-14 h-14 rounded-full bg-coral/20 flex items-center justify-center mx-auto mb-4">
            <span className="text-2xl text-coral">✕</span>
          </div>

          <h1 className="font-display text-xl text-ink mb-2">
            Verification failed
          </h1>

          <p className="text-ink/60 mb-6">
            {errorMessage}
          </p>

          <a
            href="/signup"
            className="inline-block bg-ink text-white px-6 py-2 rounded-full text-sm font-semibold hover:bg-violet transition"
          >
            Back to signup
          </a>
        </div>
      )}
    </div>
  </div>
)
}