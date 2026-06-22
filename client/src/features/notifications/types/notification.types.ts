export type NotificationStatus = 'Pending' | 'Processing' | 'Sent' | 'Failed' | 'Retrying' | 'DeadLetter'
export type NotificationChannel = 'Email' | 'SMS' | 'Push' | 'InApp'

export interface Notification {
  id: string
  publicId: string
  userId: string
  type: string
  channel: NotificationChannel
  status: NotificationStatus
  createdAt: string
}

export interface NotificationDetail extends Notification {
  recipient: string
  template: string
  provider: string
  retries: number
  payload: Record<string, unknown>
  logs: NotificationLog[]
}

export interface NotificationLog {
  id: string
  provider: string
  response: string
  createdAt: string
}