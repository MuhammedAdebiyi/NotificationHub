export interface LoginRequest {
  email: string
  password: string
  rememberMe: boolean
}

export interface SignupRequest {
  fullName: string
  email: string
  password: string
  confirmPassword: string
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
  user: {
    id: string
    fullName: string
    email: string
  }
}