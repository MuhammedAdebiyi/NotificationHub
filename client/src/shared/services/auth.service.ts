const TOKEN_KEY = 'nh_token'

export interface AuthUser {
  userId: string
  email: string
  fullName: string
  organizationId: string
  role: string
}

interface TokenPayload {
  sub: string
  email: string
  'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name'?: string
  org_id?: string
  'http://schemas.microsoft.com/ws/2008/06/identity/claims/role'?: string
  exp?: number
}

function decodeToken(token: string): AuthUser | null {
  try {
    const payload: TokenPayload = JSON.parse(atob(token.split('.')[1]))
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

function isTokenExpired(token: string): boolean {
  try {
    const payload: TokenPayload = JSON.parse(atob(token.split('.')[1]))
    if (!payload.exp) return false
    return payload.exp * 1000 < Date.now()
  } catch {
    return true
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
    const token = this.getToken()
    if (!token) return false
    if (isTokenExpired(token)) {
      this.clearToken()
      return false
    }
    return true
  },

  getUser(): AuthUser | null {
    const token = this.getToken()
    if (!token) return null
    if (isTokenExpired(token)) {
      this.clearToken()
      return null
    }
    return decodeToken(token)
  },
}
