import AppLayout from '@/app/layouts/AppLayout'
import NotificationTable from '../components/NotificationTable'
import { useNotifications } from '../hooks/useNotifications'

export default function NotificationsPage() {
  const { notifications, totalCount, isLoading, error } = useNotifications()

  return (
    <AppLayout>
      <div className="mb-8 flex items-center justify-between">
        <div>
          <p className="hand text-xl text-violet mb-1">all activity —</p>
          <h1 className="font-display font-bold text-3xl">Notifications</h1>
        </div>
        <span className="text-sm text-ink/50">{totalCount} total</span>
      </div>

      <div className="bg-fog border border-ink/10 rounded-lg p-6">
        {isLoading && (
          <div className="text-center py-16 text-ink/40 text-sm">Loading...</div>
        )}
        {error && (
          <div className="text-center py-16 text-coral text-sm">{error}</div>
        )}
        {!isLoading && !error && (
          <NotificationTable notifications={notifications} />
        )}
      </div>
    </AppLayout>
  )
}