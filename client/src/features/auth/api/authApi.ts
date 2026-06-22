import { apiClient } from '@/shared/services/apiClient'
import type {
  LoginRequest,
  SignupRequest,
  ForgotPasswordRequest,
  ResetPasswordRequest,
  AuthResponse,
} from '../types/auth.types'

export const authApi = {
  login: (payload: LoginRequest) =>
    apiClient.post<AuthResponse>('/api/auth/login', payload),

  signup: (payload: SignupRequest) =>
    apiClient.post<AuthResponse>('/api/auth/signup', payload),

  forgotPassword: (payload: ForgotPasswordRequest) =>
    apiClient.post<{ message: string }>('/api/auth/forgot-password', payload),

  resetPassword: (payload: ResetPasswordRequest) =>
    apiClient.post<{ message: string }>('/api/auth/reset-password', payload),
}