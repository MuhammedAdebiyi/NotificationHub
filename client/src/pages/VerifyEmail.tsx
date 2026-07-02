import { useEffect, useState } from 'react'
import { useSearchParams, Link } from 'react-router-dom'
import { apiClient } from '@/shared/services/apiClient'

type VerifyStatus = 'verifying' | 'success' | 'error'

export default function VerifyEmail() {
  const [searchParams] = useSearchParams()
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
      .get(`/api/v1/auth/verify-email?token=${encodeURIComponent(token)}`)
      .then(() => setStatus('success'))
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
            <h1 className="font-space-grotesk text-xl text-ink mb-2">Verifying your email...</h1>
            <p className="text-ink/60">Hang tight, this only takes a second.</p>
          </div>
        )}

        {status === 'success' && (
          <div className="text-center">
            <div className="w-14 h-14 rounded-full bg-teal/20 flex items-center justify-center mx-auto mb-4">
              <span className="text-2xl">✓</span>
            </div>
            <h1 className="font-space-grotesk text-xl text-ink mb-2">Email verified!</h1>
            <p className="text-ink/60 mb-6">Your account is now fully active.</p>
            <Link
              to="/login"
              className="inline-block bg-violet text-paper px-6 py-2 rounded-lg font-medium hover:opacity-90 transition"
            >
              Go to login
            </Link>
          </div>
        )}

        {status === 'error' && (
          <div className="text-center">
            <div className="w-14 h-14 rounded-full bg-coral/20 flex items-center justify-center mx-auto mb-4">
              <span className="text-2xl">✕</span>
            </div>
            <h1 className="font-space-grotesk text-xl text-ink mb-2">Verification failed</h1>
            <p className="text-ink/60 mb-6">{errorMessage}</p>
            <Link
              to="/login"
              className="inline-block bg-ink text-paper px-6 py-2 rounded-lg font-medium hover:opacity-90 transition"
            >
              Back to login
            </Link>
          </div>
        )}
      </div>
    </div>
  )
}