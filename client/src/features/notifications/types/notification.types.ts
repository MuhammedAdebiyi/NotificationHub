export type NotificationStatus = 'Pending' | 'Queued' | 'Processing' | 'Sent' | 'Delivered' | 'Bounced' | 'Failed' | 'Retrying' | 'DeadLetter'
export type NotificationChannel = 'Email' | 'Sms' | 'Push' | 'InApp'

export interface Notification {
  publicId: string
  recipientEmail: string
  type: string
  channel: NotificationChannel
  status: NotificationStatus
  retryCount: number
  createdAt: string
  openedAt: string | null
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
