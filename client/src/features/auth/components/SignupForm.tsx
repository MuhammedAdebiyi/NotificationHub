import { useState } from 'react'
import { useSignup } from '../hooks/useSignup'

function validatePassword(password: string): string | null {
  if (password.length < 8) return 'Password must be at least 8 characters.'
  if (!/[A-Z]/.test(password)) return 'Password must contain an uppercase letter.'
  if (!/[a-z]/.test(password)) return 'Password must contain a lowercase letter.'
  if (!/[0-9]/.test(password)) return 'Password must contain a number.'
  return null
}

export default function SignupForm() {
  const { signup, isLoading, error, success } = useSignup()
  const [form, setForm] = useState({
    fullName: '',
    email: '',
    orgName: '',
    password: '',
    confirmPassword: '',
  })
  const [validationError, setValidationError] = useState<string | null>(null)

  function handleChange(e: React.ChangeEvent<HTMLInputElement>) {
    setForm({ ...form, [e.target.name]: e.target.value })
    setValidationError(null)
  }

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    if (form.password !== form.confirmPassword) {
      setValidationError('Passwords do not match.')
      return
    }
    const pwError = validatePassword(form.password)
    if (pwError) {
      setValidationError(pwError)
      return
    }
    signup(form)
  }

  if (success) {
    return (
      <div className="text-center py-10">
        <p className="hand text-2xl text-teal mb-2">almost there —</p>
        <h2 className="font-display font-bold text-3xl mb-3">Check your email</h2>
        <p className="text-ink/60">We sent a verification link to <strong>{form.email}</strong></p>
      </div>
    )
  }

  const displayError = validationError ?? error

  return (
    <form onSubmit={handleSubmit} className="space-y-4">
      {displayError && (
        <div className="bg-coral/10 text-coral text-sm px-4 py-3 rounded-xl">{displayError}</div>
      )}

      <input
        name="fullName"
        placeholder="Full name"
        value={form.fullName}
        onChange={handleChange}
        required
        className="w-full bg-fog border border-ink/10 rounded-xl px-4 py-3 text-sm focus:outline-none focus:ring-2 focus:ring-violet"
      />
      <input
        type="email"
        name="email"
        placeholder="Work email"
        value={form.email}
        onChange={handleChange}
        required
        className="w-full bg-fog border border-ink/10 rounded-xl px-4 py-3 text-sm focus:outline-none focus:ring-2 focus:ring-violet"
      />
      <input
        name="orgName"
        placeholder="Organization name (e.g. CourseVault)"
        value={form.orgName}
        onChange={handleChange}
        required
        className="w-full bg-fog border border-ink/10 rounded-xl px-4 py-3 text-sm focus:outline-none focus:ring-2 focus:ring-violet"
      />
      <input
        type="password"
        name="password"
        placeholder="Password (min 8 chars, uppercase, lowercase, number)"
        value={form.password}
        onChange={handleChange}
        required
        minLength={8}
        className="w-full bg-fog border border-ink/10 rounded-xl px-4 py-3 text-sm focus:outline-none focus:ring-2 focus:ring-violet"
      />
      <input
        type="password"
        name="confirmPassword"
        placeholder="Confirm password"
        value={form.confirmPassword}
        onChange={handleChange}
        required
        className="w-full bg-fog border border-ink/10 rounded-xl px-4 py-3 text-sm focus:outline-none focus:ring-2 focus:ring-violet"
      />

      <button
        type="submit"
        disabled={isLoading}
        className="w-full bg-ink text-white font-semibold text-sm py-3.5 rounded-xl hover:bg-violet transition disabled:opacity-50"
      >
        {isLoading ? 'Creating account…' : 'Create account'}
      </button>
    </form>
  )
}
