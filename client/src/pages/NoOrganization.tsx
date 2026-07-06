import { authService } from '@/shared/services/auth.service'
import { useNavigate } from 'react-router-dom'

export default function NoOrganization() {
  const navigate = useNavigate()

  function handleLogout() {
    authService.clearToken()
    navigate('/login')
  }

  return (
    <div className="min-h-screen bg-fog flex items-center justify-center px-4">
      <div className="max-w-md w-full bg-paper rounded-2xl shadow-lg p-10 text-center">
        <div className="w-16 h-16 rounded-full bg-yellow/20 flex items-center justify-center mx-auto mb-6">
          <span className="text-3xl">🏢</span>
        </div>

        <p className="hand text-xl text-violet mb-2">no organization —</p>

        <h1 className="font-display font-bold text-2xl mb-3">
          You're not part of any organization
        </h1>

        <p className="text-ink/60 mb-8 leading-relaxed">
          You may have been removed, or your invite may have expired.
          Ask someone to invite you, or create a new organization.
        </p>

        <div className="flex flex-col gap-3">
          <a
            href="/signup"
            className="w-full bg-ink text-white font-semibold text-sm py-3 rounded-xl hover:bg-violet transition text-center"
          >
            Create a new organization
          </a>

          <button
            onClick={handleLogout}
            className="w-full border border-ink/20 text-ink/70 font-medium text-sm py-3 rounded-xl hover:bg-fog transition"
          >
            Log out
          </button>
        </div>
      </div>
    </div>
  )
}