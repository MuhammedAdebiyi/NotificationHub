import { BrowserRouter, Routes, Route } from 'react-router-dom'
import ProtectedRoute from '@/shared/components/ProtectedRoute'
import Landing from '@/pages/Landing'
import Login from '@/pages/Login'
import Signup from '@/pages/Signup'
import ForgotPassword from '@/pages/ForgotPassword'
import ResetPassword from '@/pages/ResetPassword'
import Dashboard from '@/pages/Dashboard'
import Notifications from '@/pages/Notifications'
import NotificationDetail from '@/pages/NotificationDetail'
import Templates from '@/pages/Templates'
import TemplateEditor from '@/pages/TemplateEditor'
import Campaigns from '@/pages/Campaigns'
import Analytics from '@/pages/Analytics'
import Users from '@/pages/Users'
import NoOrganization from '@/pages/NoOrganization'
import AcceptInvite from '@/pages/AcceptInvite'
import TeamMember from '@/pages/TeamMember'
import Settings from '@/pages/Settings'
import SelectOrg from '@/pages/SelectOrg'
import VerifyEmail from '@/pages/VerifyEmail'
import AccessRevoked from '@/pages/AccessRevoked'
import CampaignDetail from '@/pages/CampaignDetail'



export default function AppRouter() {
  return (
    <BrowserRouter>
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
        <Route path="/analytics" element={<ProtectedRoute><Analytics /></ProtectedRoute>} />
        <Route path="/campaigns/:id" element={<ProtectedRoute><CampaignDetail /></ProtectedRoute>} />
        <Route path="/team/:memberId" element={<ProtectedRoute><TeamMember /></ProtectedRoute>} />
        <Route path="/users" element={<ProtectedRoute><Users /></ProtectedRoute>} />
        <Route path="/verify-email" element={<VerifyEmail />} />
        <Route path="/settings" element={<ProtectedRoute><Settings /></ProtectedRoute>} />
        
      </Routes>
    </BrowserRouter>
  )
}