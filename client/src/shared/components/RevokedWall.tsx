import { authService } from '@/shared/services/auth.service'
import { useNavigate } from 'react-router-dom'

export default function RevokedWall({ orgName }: { orgName?: string }) {
  const navigate = useNavigate()

  function handleSignOut() {
    authService.clearToken()
    navigate('/login')
  }

  return (
    <div className="min-h-screen bg-paper flex items-center justify-center px-4">
      <div className="max-w-md text-center">
        <div className="w-16 h-16 rounded-full bg-coral/20 flex items-center justify-center mx-auto mb-6">
          <span className="text-3xl">🚫</span>
        </div>
        <h1 className="font-display font-bold text-2xl mb-3">Your access has been revoked</h1>
        <p className="text-ink/60 mb-8">
          Your access to <strong>{orgName ?? "this organization"}</strong> has been revoked by an admin.
          Contact your organization owner if you think this is a mistake.
        </p>
        <button
          onClick={handleSignOut}
          className="px-6 py-3 bg-ink text-white rounded-xl font-medium text-sm hover:bg-violet transition"
        >
          Sign out
        </button>
      </div>
    </div>
  )
}