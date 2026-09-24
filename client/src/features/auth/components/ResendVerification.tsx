import { useEffect, useRef, useState } from 'react'
import { authApi } from '../api/authApi'

interface Props {
  /** Known email — hides the input and pre-fills the request. */
  email?: string
  /** Seconds before another resend is allowed (matches server rate limit). */
  cooldownSeconds?: number
}

type SendState = 'idle' | 'sending' | 'sent'

export default function ResendVerification({ email, cooldownSeconds = 60 }: Props) {
  const [inputEmail, setInputEmail] = useState('')
  const [state, setState] = useState<SendState>('idle')
  const [error, setError] = useState<string | null>(null)
  const [cooldown, setCooldown] = useState(0)
  const timerRef = useRef<ReturnType<typeof setInterval> | null>(null)

  useEffect(() => () => {
    if (timerRef.current) clearInterval(timerRef.current)
  }, [])

  function startCooldown() {
    setCooldown(cooldownSeconds)
    if (timerRef.current) clearInterval(timerRef.current)
    timerRef.current = setInterval(() => {
      setCooldown((prev) => {
        if (prev <= 1 && timerRef.current) {
          clearInterval(timerRef.current)
          timerRef.current = null
          return 0
        }
        return prev - 1
      })
    }, 1000)
  }

  async function handleResend(e?: React.FormEvent) {
    e?.preventDefault()
    const target = email ?? inputEmail.trim()
    if (!target || state === 'sending' || cooldown > 0) return

    setState('sending')
    setError(null)
    try {
      await authApi.resendVerification(target)
      setState('sent')
      startCooldown()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Could not send the email. Try again.')
      setState('idle')
    }
  }

  if (state === 'sent') {
    return (
      <div className="text-center space-y-3">
        <div className="bg-teal/10 text-teal text-sm px-4 py-3 rounded-xl">
          Link sent — check your inbox{email ? ` for ${email}` : ''}.
        </div>
        <button
          type="button"
          onClick={() => handleResend()}
          disabled={cooldown > 0}
          className="text-violet text-sm font-medium hover:underline disabled:opacity-50 disabled:no-underline"
        >
          {cooldown > 0 ? `Resend again in ${cooldown}s` : 'Resend again'}
        </button>
      </div>
    )
  }

  if (email) {
    return (
      <div className="text-center space-y-2">
        {error && (
          <div className="bg-coral/10 text-coral text-sm px-4 py-3 rounded-xl text-left">{error}</div>
        )}
        <button
          type="button"
          onClick={() => handleResend()}
          disabled={state === 'sending' || cooldown > 0}
          className="text-violet text-sm font-medium hover:underline disabled:opacity-50 disabled:no-underline"
        >
          {state === 'sending'
            ? 'Sending…'
            : cooldown > 0
              ? `Resend again in ${cooldown}s`
              : "Didn't get it? Resend email"}
        </button>
      </div>
    )
  }

  return (
    <form onSubmit={handleResend} className="space-y-3">
      {error && (
        <div className="bg-coral/10 text-coral text-sm px-4 py-3 rounded-xl">{error}</div>
      )}
      <input
        type="email"
        placeholder="Your email address"
        value={inputEmail}
        onChange={(e) => setInputEmail(e.target.value)}
        required
        className="w-full bg-fog border border-ink/10 rounded-xl px-4 py-3 text-sm focus:outline-none focus:ring-2 focus:ring-violet"
      />
      <button
        type="submit"
        disabled={state === 'sending' || cooldown > 0}
        className="w-full bg-ink text-white font-semibold text-sm py-3 rounded-xl hover:bg-violet transition disabled:opacity-50"
      >
        {state === 'sending'
          ? 'Sending…'
          : cooldown > 0
            ? `Resend in ${cooldown}s`
            : 'Send verification link'}
      </button>
    </form>
  )
}
