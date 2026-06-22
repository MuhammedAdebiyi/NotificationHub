import AppLayout from '@/app/layouts/AppLayout'
import KpiCard from '../components/KpiCard'
import ActivityFeed from '../components/ActivityFeed'
import QueueMetrics from '../components/QueueMetrics'
import { useDashboardStats } from '../hooks/useDashboardStats'

export default function DashboardPage() {
  const { stats } = useDashboardStats()

  return (
    <AppLayout>
      <div className="mb-8">
        <p className="hand text-xl text-violet mb-1">system status —</p>
        <h1 className="font-display font-bold text-3xl">Dashboard</h1>
      </div>

      <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-6 gap-4 mb-10">
        <KpiCard label="Total Sent" value={stats.totalSent} accent="teal" rotate="-1deg" />
        <KpiCard label="Pending" value={stats.pending} accent="yellow" rotate="1deg" />
        <KpiCard label="Failed" value={stats.failed} accent="coral" rotate="-0.5deg" />
        <KpiCard label="Success Rate" value={`${stats.successRate}%`} accent="violet" rotate="1.5deg" />
        <KpiCard label="Queue Length" value={stats.queueLength} accent="teal" rotate="-1.5deg" />
        <KpiCard label="Active Users" value={stats.activeUsers} accent="coral" rotate="1deg" />
      </div>

      <div className="grid lg:grid-cols-3 gap-6">
        <QueueMetrics />
        <ActivityFeed />
      </div>
    </AppLayout>
  )
}