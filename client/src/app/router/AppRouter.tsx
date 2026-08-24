import { lazy, Suspense } from 'react'
import { BrowserRouter, Routes, Route } from 'react-router-dom'
import ProtectedRoute from '@/shared/components/ProtectedRoute'

const Landing = lazy(() => import('@/pages/Landing'))
const Login = lazy(() => import('@/pages/Login'))
const Signup = lazy(() => import('@/pages/Signup'))
const ForgotPassword = lazy(() => import('@/pages/ForgotPassword'))
const ResetPassword = lazy(() => import('@/pages/ResetPassword'))
const Dashboard = lazy(() => import('@/pages/Dashboard'))
const Notifications = lazy(() => import('@/pages/Notifications'))
const NotificationDetail = lazy(() => import('@/pages/NotificationDetail'))
const Templates = lazy(() => import('@/pages/Templates'))
const TemplateEditor = lazy(() => import('@/pages/TemplateEditor'))
const Campaigns = lazy(() => import('@/pages/Campaigns'))
const CampaignDetail = lazy(() => import('@/pages/CampaignDetail'))
const Analytics = lazy(() => import('@/pages/Analytics'))
const Users = lazy(() => import('@/pages/Users'))
const TeamMember = lazy(() => import('@/pages/TeamMember'))
const Settings = lazy(() => import('@/pages/Settings'))
const SelectOrg = lazy(() => import('@/pages/SelectOrg'))
const VerifyEmail = lazy(() => import('@/pages/VerifyEmail'))
const AccessRevoked = lazy(() => import('@/pages/AccessRevoked'))
const AcceptInvite = lazy(() => import('@/pages/AcceptInvite'))
const NoOrganization = lazy(() => import('@/pages/NoOrganization'))
const DataSourcesPage = lazy(() => import('@/features/datasources/pages/DataSourcesPage'))
const NotFoundPage = lazy(() => import('@/app/pages/NotFoundPage'))

function SuspenseSpinner() {
  return (
    <div className="min-h-screen bg-paper flex items-center justify-center">
      <div className="text-sm text-ink/40">Loading...</div>
    </div>
  )
}

export default function AppRouter() {
  return (
    <BrowserRouter>
      <Suspense fallback={<SuspenseSpinner />}>
        <Routes>
          {/* Public routes */}
          <Route path="/" element={<Landing />} />
          <Route path="/login" element={<Login />} />
          <Route path="/signup" element={<Signup />} />
          <Route path="/forgot-password" element={<ForgotPassword />} />
          <Route path="/select-org" element={<SelectOrg />} />
          <Route path="/reset-password" element={<ResetPassword />} />
          <Route path="/accept-invite" element={<AcceptInvite />} />
          <Route path="/no-organization" element={<NoOrganization />} />
          <Route path="/access-revoked" element={<AccessRevoked />} />

          {/* Protected routes */}
          <Route path="/dashboard" element={<ProtectedRoute><Dashboard /></ProtectedRoute>} />
          <Route path="/notifications" element={<ProtectedRoute><Notifications /></ProtectedRoute>} />
          <Route path="/notifications/:id" element={<ProtectedRoute><NotificationDetail /></ProtectedRoute>} />
          <Route path="/templates" element={<ProtectedRoute><Templates /></ProtectedRoute>} />
          <Route path="/templates/:id" element={<ProtectedRoute><TemplateEditor /></ProtectedRoute>} />
          <Route path="/campaigns" element={<ProtectedRoute><Campaigns /></ProtectedRoute>} />
          <Route path="/campaigns/:id" element={<ProtectedRoute><CampaignDetail /></ProtectedRoute>} />
          <Route path="/analytics" element={<ProtectedRoute><Analytics /></ProtectedRoute>} />
          <Route path="/users" element={<ProtectedRoute><Users /></ProtectedRoute>} />
          <Route path="/team/:memberId" element={<ProtectedRoute><TeamMember /></ProtectedRoute>} />
          <Route path="/settings" element={<ProtectedRoute><Settings /></ProtectedRoute>} />
          <Route path="/settings/data-sources" element={<ProtectedRoute><DataSourcesPage /></ProtectedRoute>} />
          <Route path="/verify-email" element={<VerifyEmail />} />

          {/* 404 catch-all */}
          <Route path="*" element={<NotFoundPage />} />
        </Routes>
      </Suspense>
    </BrowserRouter>
  )
}
