import { useState } from 'react'
import { authApi } from '../api/authApi'

export default function ForgotPasswordForm() {
  const [email, setEmail] = useState('')
  const [sent, setSent] = useState(false)
  const [isLoading, setIsLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setIsLoading(true)
    setError(null)
    try {
      await authApi.forgotPassword({ email })
      setSent(true)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Something went wrong.')
    } finally {
      setIsLoading(false)
    }
  }

  if (sent) {
    return (
      <div className="text-center">
        <p className="hand text-2xl text-teal mb-2">check your inbox —</p>
        <h2 className="font-display font-bold text-3xl mb-3">Reset link sent</h2>
        <p className="text-ink/60">If an account exists for {email}, a reset link is on its way.</p>
      </div>
    )
  }

  return (
    <form onSubmit={handleSubmit} className="space-y-4 text-left">
      {error && (
        <div className="bg-coral/10 text-coral text-sm px-4 py-3 rounded-xl">
          {error}
        </div>
      )}
      <input
        type="email"
        placeholder="Email address"
        value={email}
        onChange={(e) => setEmail(e.target.value)}
        required
        className="w-full bg-fog border border-ink/10 rounded-xl px-4 py-3 text-sm focus:outline-none focus:ring-2 focus:ring-violet"
      />
      <button
        type="submit"
        disabled={isLoading}
        className="w-full bg-ink text-white font-semibold text-sm py-3.5 rounded-xl hover:bg-violet transition disabled:opacity-50"
      >
        {isLoading ? 'Sending…' : 'Send reset link'}
      </button>
    </form>
  )
}