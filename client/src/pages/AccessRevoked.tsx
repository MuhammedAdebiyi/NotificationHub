import { authService } from '@/shared/services/auth.service'
import { useNavigate } from 'react-router-dom'

export default function AccessRevokedPage() {
  const navigate = useNavigate()
  const user = authService.getUser()
  const firstName = user?.fullName?.split(' ')[0] ?? 'there'

  function handleSignOut() {
    authService.clearToken()
    navigate('/login')
  }

  return (
    <div className="min-h-screen bg-paper flex items-center justify-center px-4">
      <div className="w-full max-w-md text-center">
        <div className="w-16 h-16 rounded-full bg-coral/10 flex items-center justify-center mx-auto mb-6">
          <span className="text-coral text-2xl">⊘</span>
        </div>
        <p className="hand text-xl text-coral mb-2">access revoked —</p>
        <h1 className="font-display font-bold text-3xl mb-4">
          Hi {firstName}, your access has been revoked
        </h1>
        <p className="text-sm text-ink/50 mb-8">
          Your access to this organization has been revoked by an admin.
          If you think this is a mistake, reach out to your organization owner.
        </p>
        <button
          onClick={handleSignOut}
          className="px-6 py-3 bg-ink text-white rounded-xl text-sm font-semibold hover:bg-violet transition"
        >
          Sign out
        </button>
      </div>
    </div>
  )
}