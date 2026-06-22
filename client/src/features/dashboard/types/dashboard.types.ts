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
  timestamp: string
}   