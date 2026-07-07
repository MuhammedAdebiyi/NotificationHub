import AppLayout from '@/app/layouts/AppLayout'
import KpiCard from '../components/KpiCard'
import ActivityFeed from '../components/ActivityFeed'
import QueueMetrics from '../components/QueueMetrics'
import { useDashboardStats } from '../hooks/useDashboardStats'
import { authService } from '@/shared/services/auth.service'
import { useState, useEffect } from 'react'
import { dashboardApi } from '../api/dashboardApi'

const roleStyle: Record<string, string> = {
  owner: 'bg-violet/10 text-violet',
  admin: 'bg-teal/10 text-teal',
  member: 'bg-fog border border-ink/10 text-ink/60',
  revoked: 'bg-coral/10 text-coral',
}

function getGreeting(): string {
  const hour = new Date().getHours()
  if (hour < 12) return 'Good morning'
  if (hour < 17) return 'Good afternoon'
  return 'Good evening'
}

export default function DashboardPage() {
  const { stats, activity } = useDashboardStats()
  const user = authService.getUser()
  const firstName = user?.fullName?.split(' ')[0] ?? 'there'
  const role = user?.role ?? 'member'

  const [orgName, setOrgName] = useState<string>('')

  useEffect(() => {
    dashboardApi.getOrgInfo()
      .then(res => setOrgName(res.name))
      .catch(() => {})
  }, [])

  return (
    <AppLayout>
      {/* Greeting header */}
      <div className="mb-8">
        <div className="flex items-start justify-between">
          <div>
            <p className="hand text-2xl text-violet mb-1">
              {getGreeting()}, {firstName} —
            </p>
            <h1 className="font-display font-bold text-3xl">Dashboard</h1>
            <div className="flex items-center gap-3 mt-2">
              {orgName && (
                <div className="flex items-center gap-2">
                  <span className="text-xs text-ink/40">Organization</span>
                  <span className="text-xs font-semibold bg-ink text-white px-2.5 py-1 rounded-full">
                    {orgName}
                  </span>
                </div>
              )}
              <span className={`text-xs font-medium px-2.5 py-1 rounded-full ${roleStyle[role] ?? roleStyle.member}`}>
                {role}
              </span>
            </div>
          </div>

          {/* User card */}
          <div className="hidden sm:flex items-center gap-3 bg-fog border border-ink/10 rounded-xl px-4 py-3">
            <div className="w-9 h-9 rounded-full bg-violet/10 text-violet font-display font-bold text-sm flex items-center justify-center">
              {user?.fullName?.charAt(0) ?? '?'}
            </div>
            <div>
              <p className="text-sm font-semibold">{user?.fullName}</p>
              <p className="text-xs text-ink/40">{user?.email}</p>
            </div>
          </div>
        </div>
      </div>

      {/* KPI cards */}
      <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-6 gap-4 mb-10">
        <KpiCard label="Total Sent" value={stats.totalSent} accent="teal" rotate="-1deg" />
        <KpiCard label="Pending" value={stats.pending} accent="yellow" rotate="1deg" />
        <KpiCard label="Failed" value={stats.failed} accent="coral" rotate="-0.5deg" />
        <KpiCard label="Success Rate" value={`${stats.successRate}%`} accent="violet" rotate="1.5deg" />
        <KpiCard label="Queue Length" value={stats.queueLength} accent="teal" rotate="-1.5deg" />
        <KpiCard label="Active Users" value={stats.activeUsers} accent="coral" rotate="1deg" />
      </div>

      {/* Charts + Activity */}
      <div className="grid lg:grid-cols-3 gap-6">
        <QueueMetrics />
        <ActivityFeed items={activity} />
      </div>
    </AppLayout>
  )
}