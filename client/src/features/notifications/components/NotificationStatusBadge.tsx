import type { NotificationStatus } from '../types/notification.types'

const statusStyles: Record<NotificationStatus, string> = {
  Pending: 'bg-yellow/20 text-ink',
  Processing: 'bg-violet/10 text-violet',
  Sent: 'bg-teal/10 text-teal',
  Failed: 'bg-coral/10 text-coral',
  Retrying: 'bg-yellow/20 text-ink',
  DeadLetter: 'bg-ink/10 text-ink/60',
}

export default function NotificationStatusBadge({ status }: { status: NotificationStatus }) {
  return (
    <span className={`text-xs font-medium px-3 py-1 rounded-full ${statusStyles[status]}`}>
      {status}
    </span>
  )
}