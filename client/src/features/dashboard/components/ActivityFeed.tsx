import type { ActivityItem } from '../types/dashboard.types'

export default function ActivityFeed({ items = [] }: { items?: ActivityItem[] }) {
  return (
    <div className="bg-fog border border-ink/10 rounded-lg p-6">
      <h2 className="font-display font-bold text-lg mb-4">Recent Activity</h2>
      {items.length === 0 ? (
        <p className="text-sm text-ink/40 text-center py-6">No activity yet.</p>
      ) : (
        <ul className="space-y-3">
          {items.map((item) => (
            <li key={item.id} className="flex items-center justify-between text-sm border-b border-ink/10 pb-3 last:border-0 last:pb-0">
              <div>
                <span className="text-ink/80 font-medium">{item.label}</span>
                <span className="text-ink/40 text-xs ml-2">{item.channel}</span>
              </div>
              <div className="text-right shrink-0 ml-4">
                <span className="text-ink/40 text-xs block">
                  {new Date(item.timestamp).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
                </span>
                <span className={`text-xs ${item.status === 'Sent' ? 'text-teal' : item.status === 'Failed' ? 'text-coral' : 'text-ink/40'}`}>
                  {item.status}
                </span>
              </div>
            </li>
          ))}
        </ul>
      )}
    </div>
  )
}