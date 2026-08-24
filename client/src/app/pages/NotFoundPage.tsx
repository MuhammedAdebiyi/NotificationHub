import { useNavigate } from 'react-router-dom'

export default function NotFoundPage() {
  const navigate = useNavigate()
  return (
    <div className="min-h-screen bg-paper flex items-center justify-center px-4">
      <div className="max-w-md w-full text-center">
        <p className="hand text-4xl text-violet mb-2">404</p>
        <h1 className="font-display font-bold text-3xl mb-3">Page not found</h1>
        <p className="text-ink/50 text-sm mb-8">
          The page you're looking for doesn't exist or has been moved.
        </p>
        <button
          onClick={() => navigate('/dashboard')}
          className="px-5 py-2.5 bg-ink text-white rounded-lg text-sm font-medium hover:bg-ink/80 transition"
        >
          Go to dashboard
        </button>
      </div>
    </div>
  )
}
