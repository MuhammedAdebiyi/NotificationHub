import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { authApi } from '../api/authApi'
import { authService } from '@/shared/services/auth.service'
import type { LoginRequest } from '../types/auth.types'

export function useLogin() {
  const [isLoading, setIsLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const navigate = useNavigate()

  async function login(payload: LoginRequest) {
    setIsLoading(true)
    setError(null)

    try {
      const response = await authApi.login(payload)
      authService.setToken(response.token)
      navigate('/dashboard')
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Login failed. Please try again.')
    } finally {
      setIsLoading(false)
    }
  }

  return { login, isLoading, error }
}