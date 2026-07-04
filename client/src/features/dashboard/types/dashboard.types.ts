export interface DashboardStats {
  totalSent: number
  pending: number
  failed: number
  successRate: number
  queueLength: number
  activeUsers: number
}

export interface ActivityItem {
  id: string
  label: string
  channel: string
  status: string
  timestamp: string
}