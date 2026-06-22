import AppLayout from '@/app/layouts/AppLayout'

export default function SettingsPage() {
  return (
    <AppLayout>
      <h1 className="font-display font-bold text-3xl mb-2">Settings</h1>
      <p className="text-ink/50">Provider keys, queue config, security — coming with backend wiring.</p>
    </AppLayout>
  )
}