const TOKEN_KEY = 'nh_token'

export interface AuthUser {
  userId: string
  email: string
  fullName: string
  organizationId: string
  role: string
}

function decodeToken(token: string): AuthUser | null {
  try {
    const payload = JSON.parse(atob(token.split('.')[1]))
    return {
      userId: payload.sub,
      email: payload.email,
      fullName: payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name'] ?? '',
      organizationId: payload.org_id ?? '',
      role: payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] ?? 'member',
    }
  } catch {
    return null
  }
}

export const authService = {
  getToken(): string | null {
    return localStorage.getItem(TOKEN_KEY)
  },

  setToken(token: string): void {
    localStorage.setItem(TOKEN_KEY, token)
  },

  clearToken(): void {
    localStorage.removeItem(TOKEN_KEY)
  },

  isAuthenticated(): boolean {
    return !!this.getToken()
  },

  getUser(): AuthUser | null {
    const token = this.getToken()
    if (!token) return null
    return decodeToken(token)
  },
}