import { useNavigate } from 'react-router-dom'

const ACTIONS = [
  { label: 'Send notification', to: '/notifications/new' },
  { label: 'New campaign',      to: '/campaigns/new' },
  { label: 'New template',      to: '/templates/new' },
  { label: 'Invite member',     to: '/users' },
  { label: 'Create API key',    to: '/settings' },
  { label: 'View queue',        to: '/analytics' },
]

export default function QuickActions() {
  const navigate = useNavigate()
  return (
    <div>
      <p className="text-[11px] font-medium text-ink/40 uppercase tracking-wide mb-2">
        Quick actions
      </p>
      <div className="flex flex-wrap gap-2">
        {ACTIONS.map(({ label, to }) => (
          <button
            key={label}
            onClick={() => navigate(to)}
            className="px-3 py-1.5 rounded-lg border border-ink/10 bg-white text-xs text-ink/60 hover:bg-fog hover:text-ink transition-colors"
          >
            {label}
          </button>
        ))}
      </div>
    </div>
  )
}