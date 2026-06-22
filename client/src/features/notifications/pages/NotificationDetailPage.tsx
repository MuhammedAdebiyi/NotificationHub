import { useParams } from 'react-router-dom'
import AppLayout from '@/app/layouts/AppLayout'

export default function NotificationDetailPage() {
  const { id } = useParams()
  return (
    <AppLayout>
      <h1 className="font-display font-bold text-3xl mb-2">Notification #{id}</h1>
      <p className="text-ink/50">Recipient, template, provider, retries, logs, payload, status timeline.</p>
    </AppLayout>
  )
}