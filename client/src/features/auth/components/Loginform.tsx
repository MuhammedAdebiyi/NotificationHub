import { useState } from 'react'
import { useLogin } from '../hooks/useLogin'

export default function LoginForm() {
  const { login, isLoading, error } = useLogin()
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [rememberMe, setRememberMe] = useState(false)

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    login({ email, password, rememberMe })
  }

  return (
    <form onSubmit={handleSubmit} className="space-y-4">
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
      <input
        type="password"
        placeholder="Password"
        value={password}
        onChange={(e) => setPassword(e.target.value)}
        required
        className="w-full bg-fog border border-ink/10 rounded-xl px-4 py-3 text-sm focus:outline-none focus:ring-2 focus:ring-violet"
      />

      <div className="flex items-center justify-between text-sm">
        <label className="flex items-center gap-2 text-ink/70">
          <input
            type="checkbox"
            checked={rememberMe}
            onChange={(e) => setRememberMe(e.target.checked)}
            className="rounded"
          />
          Remember me
        </label>
        <a href="/forgot-password" className="text-violet font-medium hover:underline">
          Forgot password?
        </a>
      </div>

      <button
        type="submit"
        disabled={isLoading}
        className="w-full bg-ink text-white font-semibold text-sm py-3.5 rounded-xl hover:bg-violet transition disabled:opacity-50"
      >
        {isLoading ? 'Logging in…' : 'Log in'}
      </button>
    </form>
  )
}