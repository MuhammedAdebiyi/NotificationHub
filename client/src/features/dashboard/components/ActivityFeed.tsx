import type { ActivityItem } from '../types/dashboard.types'

const FALLBACK_ACTIVITY: ActivityItem[] = [
  { id: '1', label: 'Welcome Email Sent', timestamp: '2 min ago' },
  { id: '2', label: 'Password Reset Sent', timestamp: '14 min ago' },
  { id: '3', label: 'Campaign Started', timestamp: '1 hr ago' },
  { id: '4', label: 'Retry Executed', timestamp: '2 hr ago' },
]

export default function ActivityFeed({ items = FALLBACK_ACTIVITY }: { items?: ActivityItem[] }) {
  return (
    <div className="bg-fog border border-ink/10 rounded-lg p-6">
      <h2 className="font-display font-bold text-lg mb-4">Recent Activity</h2>
      <ul className="space-y-3">
        {items.map((item) => (
          <li key={item.id} className="flex items-center justify-between text-sm border-b border-ink/10 pb-3 last:border-0 last:pb-0">
            <span className="text-ink/80">{item.label}</span>
            <span className="text-ink/40 text-xs">{item.timestamp}</span>
          </li>
        ))}
      </ul>
    </div>
  )
}