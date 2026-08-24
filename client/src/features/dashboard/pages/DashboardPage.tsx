import AppLayout from '@/app/layouts/AppLayout'
import { authService } from '@/shared/services/auth.service'
import { useDashboardData } from '../hooks/useDashboardData'

import HealthBanner          from '../components/HealthBanner'
import QuickActions          from '../components/QuickActions'
import TodayKpis             from '../components/TodayKpis'
import DeliveryChart         from '../components/DeliveryChart'
import QueueMonitor          from '../components/QueueMonitor'
import CampaignSnapshot      from '../components/CampaignSnapshot'
import RecentFailures        from '../components/RecentFailures'
import LiveActivity          from '../components/LiveActivity'
import InfrastructureStatus  from '../components/InfrastructureStatus'
import PlanUsage             from '../components/PlanUsage'

function getGreeting(): string {
  const h = new Date().getHours()
  if (h < 12) return 'Good morning'
  if (h < 17) return 'Good afternoon'
  return 'Good evening'
}

export default function DashboardPage() {
  const { health, stats, timeline, queue, campaigns, failures, activity, infra, orgInfo } = useDashboardData()

  const user = authService.getUser()
  const firstName = user?.fullName?.split(' ')[0] ?? 'there'

  return (
    <AppLayout>
      {/* Header */}
      <div className="flex items-start justify-between mb-6">
        <div>
          <p className="hand text-2xl text-violet mb-1">
            {getGreeting()}, {firstName} —
          </p>
          <h1 className="font-display font-bold text-3xl">{orgInfo?.name ?? 'Operations Center'}</h1>
        </div>
      </div>

      {/* Stack */}
      <div className="flex flex-col gap-5">
        <HealthBanner data={health} />
        <QuickActions />
        <TodayKpis stats={stats} />

        <div className="grid lg:grid-cols-[1fr_280px] gap-4">
          <DeliveryChart data={timeline} />
          <QueueMonitor data={queue} />
        </div>

        <div className="grid lg:grid-cols-2 gap-4">
          <CampaignSnapshot data={campaigns} />
          <RecentFailures data={failures} />
        </div>

        <div className="grid lg:grid-cols-[1fr_260px] gap-4">
          <LiveActivity data={activity} />
          <div className="flex flex-col gap-4">
            <InfrastructureStatus data={infra} />
            <PlanUsage stats={stats} />
        </div>

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
    </AppLayout>
  )
}
