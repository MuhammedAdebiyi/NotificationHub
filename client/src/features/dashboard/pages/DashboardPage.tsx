import { useCallback } from 'react'
import AppLayout from '@/app/layouts/AppLayout'
import { authService } from '@/shared/services/auth.service'
import { dashboardApi } from '../api/dashboardApi'
import { usePolling } from '../hooks/usePolling'

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

const FAST = { intervalMs: 5_000  }   // live panels
const SLOW = { intervalMs: 30_000 }   // slower-changing data

export default function DashboardPage() {
  const user      = authService.getUser()
  const firstName = user?.fullName?.split(' ')[0] ?? 'there'

  // ─── Data fetching ─────────────────────────────────────────────
  const { data: health }    = usePolling(useCallback(() => dashboardApi.getHealth(), []),         FAST)
  const { data: stats }     = usePolling(useCallback(() => dashboardApi.getTodayStats(), []),     FAST)
  const { data: timeline }  = usePolling(useCallback(() => dashboardApi.getDeliveryTimeline(), []), FAST)
  const { data: queue }     = usePolling(useCallback(() => dashboardApi.getQueue(), []),          FAST)
  const { data: campaigns } = usePolling(useCallback(() => dashboardApi.getCampaignSnapshot(), []), SLOW)
  const { data: failures }  = usePolling(useCallback(() => dashboardApi.getRecentFailures(), []), FAST)
  const { data: activity }  = usePolling(useCallback(() => dashboardApi.getActivity(), []),       FAST)
  const { data: infra }     = usePolling(useCallback(() => dashboardApi.getInfrastructure(), []), SLOW)

  // ─── Render ────────────────────────────────────────────────────
  return (
    <AppLayout>
      {/* ── Header ─────────────────────────────────────────── */}
      <div className="flex items-start justify-between mb-6">
        <div>
          <p className="hand text-2xl text-violet mb-1">
            {getGreeting()}, {firstName} —
          </p>
          <h1 className="font-display font-bold text-3xl">Operations Center</h1>
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

      {/* ── Stack ──────────────────────────────────────────── */}
      <div className="flex flex-col gap-5">

        {/* Health banner */}
        <HealthBanner data={health} />

        {/* Quick actions */}
        <QuickActions />

        {/* KPI row */}
        <TodayKpis stats={stats} />

        {/* Delivery chart + Queue */}
        <div className="grid lg:grid-cols-[1fr_280px] gap-4">
          <DeliveryChart data={timeline} />
          <QueueMonitor data={queue} />
        </div>

        {/* Campaigns + Recent failures */}
        <div className="grid lg:grid-cols-2 gap-4">
          <CampaignSnapshot data={campaigns} />
          <RecentFailures data={failures} />
        </div>

        {/* Live activity + Infra + Plan */}
        <div className="grid lg:grid-cols-[1fr_260px] gap-4">
          <LiveActivity data={activity} />
          <div className="flex flex-col gap-4">
            <InfrastructureStatus data={infra} />
            <PlanUsage stats={stats} />
          </div>
        </div>

      </div>
    </AppLayout>
  )
}