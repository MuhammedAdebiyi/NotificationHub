export interface SignupRequest {
  fullName: string
  email: string
  password: string
  confirmPassword: string
  orgName: string
}

export interface LoginRequest {
  email: string
  password: string
  rememberMe: boolean
}

export interface ForgotPasswordRequest {
  email: string
}

export interface ResetPasswordRequest {
  token: string
  password: string
  confirmPassword: string
}

export interface AuthResponse {
  token: string
  userId: string
  email: string
  fullName?: string
  organizationId?: string
}