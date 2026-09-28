import type { NotificationStatus } from '../types/notification.types'

const statusStyles: Record<NotificationStatus, string> = {
  Pending: 'bg-yellow/20 text-ink',
  Queued: 'bg-yellow/20 text-ink',
  Processing: 'bg-violet/10 text-violet',
  Sent: 'bg-yellow/20 text-ink',
  Delivered: 'bg-teal/10 text-teal',
  Bounced: 'bg-coral/10 text-coral',
  Failed: 'bg-coral/10 text-coral',
  Retrying: 'bg-yellow/20 text-ink',
  DeadLetter: 'bg-ink/10 text-ink/60',
}

// Raw enum names vs. honest display labels — "Sent" only means the provider's
// API accepted the message; "Delivered" is reserved for webhook-confirmed DLR.
const statusLabels: Record<NotificationStatus, string> = {
  Pending: 'Pending',
  Queued: 'Queued',
  Processing: 'Processing',
  Sent: 'Accepted',
  Delivered: 'Delivered',
  Bounced: 'Bounced',
  Failed: 'Failed',
  Retrying: 'Retrying',
  DeadLetter: 'DeadLetter',
}

export default function NotificationStatusBadge({ status }: { status: NotificationStatus }) {
  return (
    <span className={`text-xs font-medium px-3 py-1 rounded-full ${statusStyles[status] ?? 'bg-ink/10 text-ink/60'}`}>
      {statusLabels[status] ?? status}
    </span>
  )
}