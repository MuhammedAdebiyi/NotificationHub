import { useState } from 'react'
import { authApi } from '../api/authApi'
import type { SignupRequest } from '../types/auth.types'

export function useSignup() {
  const [isLoading, setIsLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [success, setSuccess] = useState(false)

  async function signup(payload: SignupRequest) {
    setIsLoading(true)
    setError(null)

    try {
      await authApi.signup(payload)
      setSuccess(true)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Signup failed. Please try again.')
    } finally {
      setIsLoading(false)
    }
  }

  return { signup, isLoading, error, success }
}