export type NotificationStatus = 'Pending' | 'Processing' | 'Sent' | 'Failed' | 'Retrying' | 'DeadLetter'
export type NotificationChannel = 'Email' | 'Sms' | 'Push' | 'InApp'

export interface Notification {
  id: string
  publicId: string
  userId: string
  type: string
  channel: NotificationChannel
  status: NotificationStatus
  retryCount: number
  createdAt: string
}

export interface NotificationDetail extends Notification {
  payload: string
  logs: NotificationLog[]
}

export interface NotificationLog {
  id: string
  provider: string
  response: string
  createdAt: string
}