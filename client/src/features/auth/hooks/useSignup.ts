import { useState } from 'react'

import { authApi } from '../api/authApi'


interface SignupForm {
  fullName: string
  email: string
  orgName: string
  password: string
  confirmPassword: string
}

export function useSignup() {
  const [isLoading, setIsLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [success, setSuccess] = useState(false)
 

  async function signup(form: SignupForm) {
  setIsLoading(true)
  setError(null)

  try {
    await authApi.signup({
      fullName: form.fullName,
      email: form.email,
      password: form.password,
      confirmPassword: form.confirmPassword,
      orgName: form.orgName,
    })
    setSuccess(true)
    // No token — user must verify email first before getting access
  } catch (err) {
    setError(err instanceof Error ? err.message : 'Signup failed. Please try again.')
  } finally {
    setIsLoading(false)
  }
}

  return { signup, isLoading, error, success }
}