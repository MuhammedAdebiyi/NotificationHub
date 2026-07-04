import { useNavigate } from 'react-router-dom'
import type { Notification } from '../types/notification.types'
import NotificationStatusBadge from './NotificationStatusBadge'

export default function NotificationTable({ notifications }: { notifications: Notification[] }) {
  const navigate = useNavigate()

  if (notifications.length === 0) {
    return (
      <div className="text-center py-16 text-ink/40 text-sm">
        No notifications yet. Send one via the API.
      </div>
    )
  }

  return (
    <div className="overflow-x-auto">
      <table className="w-full text-sm">
        <thead>
          <tr className="border-b border-ink/10 text-left text-ink/50 text-xs uppercase tracking-wide">
            <th className="pb-3 pr-4 font-medium">ID</th>
            <th className="pb-3 pr-4 font-medium">Recipient</th>
            <th className="pb-3 pr-4 font-medium">Type</th>
            <th className="pb-3 pr-4 font-medium">Channel</th>
            <th className="pb-3 pr-4 font-medium">Status</th>
            <th className="pb-3 font-medium">Created</th>
          </tr>
        </thead>
        <tbody>
          {notifications.map((n) => (
            <tr
              key={n.publicId}
              onClick={() => navigate(`/notifications/${n.publicId}`)}
              className="border-b border-ink/5 hover:bg-fog/50 cursor-pointer transition"
            >
              <td className="py-3 pr-4 font-mono text-xs text-ink/40">
                {n.publicId.slice(0, 8)}...
              </td>
              <td className="py-3 pr-4 text-ink/60 text-xs">{n.recipientEmail}</td>
              <td className="py-3 pr-4 font-medium">{n.type}</td>
              <td className="py-3 pr-4 text-ink/60">{n.channel}</td>
              <td className="py-3 pr-4">
                <NotificationStatusBadge status={n.status} />
              </td>
              <td className="py-3 text-ink/40 text-xs">
                {new Date(n.createdAt).toLocaleString()}
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}