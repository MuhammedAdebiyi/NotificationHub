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
    apiClient.post<AuthResponse>('/api/v1/auth/login', payload),

  signup: (payload: SignupRequest) =>
    apiClient.post<AuthResponse>('/api/v1/auth/signup', payload),

  me: () =>
    apiClient.get<{ userId: string; email: string; fullName: string }>('/api/v1/auth/me'),

  forgotPassword: (payload: ForgotPasswordRequest) =>
    apiClient.post<{ message: string }>('/api/v1/auth/forgot-password', payload),

  resetPassword: (payload: ResetPasswordRequest) =>
    apiClient.post<{ message: string }>('/api/v1/auth/reset-password', payload),
}