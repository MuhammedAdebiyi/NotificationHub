export type CampaignStatus =
  | 'Draft'
  | 'Scheduled'
  | 'Running'
  | 'Paused'
  | 'Completed'
  | 'Cancelled'
  | 'Sent'

export type CampaignChannel = 'Email' | 'Sms' | 'Push' | 'InApp'

export interface CampaignNotification {
  publicId: string
  recipientEmail: string
  type: string
  channel: string
  status: string
  retryCount: number
  createdAt: string
}

export interface Campaign {
  id: string
  title: string
  subject: string
  channel: CampaignChannel
  status: CampaignStatus
  totalRecipients: number
  scheduledAt: string | null
  createdAt: string
  recipientCount: number
}

export interface CampaignDetail extends Campaign {
  message: string
  stats: {
    sent: number
    pending: number
    total: number
  }
}

export interface CreateCampaignPayload {
  title: string
  subject: string
  body?: string
  templateId?: string
  channel: number
  scheduledAt?: string
}