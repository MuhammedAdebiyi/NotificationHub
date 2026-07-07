import { useNavigate } from 'react-router-dom'
import { authService } from '@/shared/services/auth.service'

export default function NoOrganizationPage() {
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
        <div className="w-16 h-16 rounded-full bg-violet/10 flex items-center justify-center mx-auto mb-6">
          <span className="text-violet text-2xl">⬡</span>
        </div>
        <p className="hand text-xl text-violet mb-2">no organization —</p>
        <h1 className="font-display font-bold text-3xl mb-4">
          Hi {firstName}, you're not part of any organization
        </h1>
        <p className="text-sm text-ink/50 mb-8">
          Ask someone to invite you, or create a new organization to get started.
        </p>
        <div className="flex flex-col sm:flex-row gap-3 justify-center">
          <button
            onClick={() => navigate('/signup')}
            className="px-6 py-3 bg-ink text-white rounded-xl text-sm font-semibold hover:bg-violet transition"
          >
            Create an organization
          </button>
          <button
            onClick={handleSignOut}
            className="px-6 py-3 border border-ink/20 text-ink/60 rounded-xl text-sm font-medium hover:bg-fog transition"
          >
            Sign out
          </button>
        </div>
      </div>
    </div>
  )
}