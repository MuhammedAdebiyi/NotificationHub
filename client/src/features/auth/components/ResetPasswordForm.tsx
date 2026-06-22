import { useState } from 'react'
import { authApi } from '../api/authApi'

export default function ResetPasswordForm() {
  const [password, setPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [isLoading, setIsLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setIsLoading(true)
    setError(null)

    const token = new URLSearchParams(window.location.search).get('token') ?? ''

    try {
      await authApi.resetPassword({ token, password, confirmPassword })
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Something went wrong.')
    } finally {
      setIsLoading(false)
    }
  }

  return (
    <form onSubmit={handleSubmit} className="space-y-4">
      {error && (
        <div className="bg-coral/10 text-coral text-sm px-4 py-3 rounded-xl">
          {error}
        </div>
      )}
      <input
        type="password"
        placeholder="New password"
        value={password}
        onChange={(e) => setPassword(e.target.value)}
        required
        className="w-full bg-fog border border-ink/10 rounded-xl px-4 py-3 text-sm focus:outline-none focus:ring-2 focus:ring-violet"
      />
      <input
        type="password"
        placeholder="Confirm new password"
        value={confirmPassword}
        onChange={(e) => setConfirmPassword(e.target.value)}
        required
        className="w-full bg-fog border border-ink/10 rounded-xl px-4 py-3 text-sm focus:outline-none focus:ring-2 focus:ring-violet"
      />
      <button
        type="submit"
        disabled={isLoading}
        className="w-full bg-ink text-white font-semibold text-sm py-3.5 rounded-xl hover:bg-violet transition disabled:opacity-50"
      >
        {isLoading ? 'Resetting…' : 'Reset password'}
      </button>
    </form>
  )
}