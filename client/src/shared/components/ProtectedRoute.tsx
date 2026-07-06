import { Navigate } from 'react-router-dom'
import { authService } from '@/shared/services/auth.service'
import type { ReactNode } from 'react'

function getOrgIdFromToken(): string | null {
  const token = authService.getToken()
  if (!token) return null
  try {
    const payload = JSON.parse(atob(token.split('.')[1]))
    return payload.org_id ?? null
  } catch {
    return null
  }
}

export default function ProtectedRoute({ children }: { children: ReactNode }) {
  if (!authService.isAuthenticated()) {
    return <Navigate to="/login" replace />
  }

  const orgId = getOrgIdFromToken()
  if (!orgId) {
    return <Navigate to="/no-organization" replace />
  }

  return <>{children}</>
}