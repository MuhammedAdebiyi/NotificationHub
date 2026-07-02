import { Navigate } from 'react-router-dom'
import { authService } from '@/shared/services/auth.service'
import type { ReactNode } from 'react'

export default function RequireAuth({ children }: { children: ReactNode }) {
  if (!authService.isAuthenticated()) {
    return <Navigate to="/login" replace />
  }
  return <>{children}</>
}