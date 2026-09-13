export interface NotificationHubConfig {
  apiKey: string;
  baseUrl?: string;
}

export interface SendNotificationParams {
  recipientEmail: string;
  type: string;
  channel: 'email' | 'sms' | 'push' | 'inapp';
  payload: string;
  idempotencyKey?: string;
}

export interface Notification {
  publicId: string;
  recipientEmail: string;
  type: string;
  channel: string;
  status: string;
  payload: string;
  createdAt: string;
  processedAt?: string;
  provider?: string;
  providerMessageId?: string;
  lastError?: string;
  retryCount: number;
  openedAt?: string;
}

export interface PaginatedResponse<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
}

export interface Template {
  id: string;
  name: string;
  subject: string;
  body?: string;
  createdAt: string;
}

export interface Campaign {
  id: string;
  title: string;
  subject: string;
  channel: string;
  status: string;
  createdAt: string;
  scheduledAt?: string;
  sentAt?: string;
}

export interface CampaignProgress {
  totalRecipients: number;
  sent: number;
  pending: number;
  processing: number;
  retrying: number;
  failed: number;
  deadLetter: number;
  progressPercent: number;
}

export class NotificationHubError extends Error {
  constructor(
    message: string,
    public statusCode: number,
    public body?: unknown,
  ) {
    super(message);
    this.name = 'NotificationHubError';
  }
}

export class NotificationHub {
  private apiKey: string;
  private baseUrl: string;

  constructor(config: NotificationHubConfig) {
    if (!config.apiKey) {
      throw new Error('API key is required. Get one at https://notificationhub.space/settings');
    }
    this.apiKey = config.apiKey;
    this.baseUrl = (config.baseUrl || 'https://api.notificationhub.space').replace(/\/$/, '');
  }

  private async request<T>(method: string, path: string, body?: unknown): Promise<T> {
    const headers: Record<string, string> = {
      'X-Api-Key': this.apiKey,
      'Content-Type': 'application/json',
    };

    const res = await fetch(`${this.baseUrl}${path}`, {
      method,
      headers,
      body: body ? JSON.stringify(body) : undefined,
    });

    const data = await res.json();

    if (!res.ok) {
      throw new NotificationHubError(
        data.error || `HTTP ${res.status}`,
        res.status,
        data,
      );
    }

    return data as T;
  }

  // ── Notifications ──────────────────────────────────────────

  async send(params: SendNotificationParams): Promise<{ publicId: string }> {
    const headers: Record<string, string> = {
      'X-Api-Key': this.apiKey,
      'Content-Type': 'application/json',
    };

    if (params.idempotencyKey) {
      headers['Idempotency-Key'] = params.idempotencyKey;
    }

    const res = await fetch(`${this.baseUrl}/api/v1/notifications`, {
      method: 'POST',
      headers,
      body: JSON.stringify({
        recipientEmail: params.recipientEmail,
        type: params.type,
        channel: params.channel,
        payload: params.payload,
      }),
    });

    const data = await res.json();

    if (!res.ok) {
      throw new NotificationHubError(
        data.error || `HTTP ${res.status}`,
        res.status,
        data,
      );
    }

    return data;
  }

  async getNotification(publicId: string): Promise<Notification> {
    return this.request<Notification>('GET', `/api/v1/notifications/${publicId}`);
  }

  async listNotifications(params?: {
    page?: number;
    pageSize?: number;
    dateFrom?: string;
    dateTo?: string;
    status?: string;
  }): Promise<PaginatedResponse<Notification>> {
    const query = new URLSearchParams();
    if (params?.page) query.set('page', String(params.page));
    if (params?.pageSize) query.set('pageSize', String(params.pageSize));
    if (params?.dateFrom) query.set('dateFrom', params.dateFrom);
    if (params?.dateTo) query.set('dateTo', params.dateTo);
    if (params?.status) query.set('status', params.status);

    const qs = query.toString();
    return this.request<PaginatedResponse<Notification>>('GET', `/api/v1/notifications${qs ? '?' + qs : ''}`);
  }

  async retryNotification(publicId: string): Promise<{ retried: boolean }> {
    return this.request<{ retried: boolean }>('POST', `/api/v1/notifications/${publicId}/retry`);
  }

  // ── Templates ──────────────────────────────────────────────

  async createTemplate(name: string, subject: string, body: string): Promise<{ id: string; name: string }> {
    return this.request<{ id: string; name: string }>('POST', '/api/v1/templates', { name, subject, body });
  }

  async getTemplate(id: string): Promise<Template> {
    return this.request<Template>('GET', `/api/v1/templates/${id}`);
  }

  async listTemplates(page = 1, pageSize = 20): Promise<PaginatedResponse<Template>> {
    return this.request<PaginatedResponse<Template>>('GET', `/api/v1/templates?page=${page}&pageSize=${pageSize}`);
  }

  async updateTemplate(id: string, name: string, subject: string, body: string): Promise<{ id: string; name: string }> {
    return this.request<{ id: string; name: string }>('PUT', `/api/v1/templates/${id}`, { name, subject, body });
  }

  async deleteTemplate(id: string): Promise<{ deleted: boolean }> {
    return this.request<{ deleted: boolean }>('DELETE', `/api/v1/templates/${id}`);
  }

  // ── Campaigns ──────────────────────────────────────────────

  async createCampaign(params: {
    title: string;
    subject: string;
    body?: string;
    templateId?: string;
    channel?: string;
    scheduledAt?: string;
  }): Promise<{ id: string; title: string }> {
    return this.request<{ id: string; title: string }>('POST', '/api/v1/campaigns', {
      title: params.title,
      subject: params.subject,
      body: params.body,
      templateId: params.templateId,
      channel: params.channel || 'email',
      scheduledAt: params.scheduledAt,
    });
  }

  async getCampaign(id: string): Promise<Campaign> {
    return this.request<Campaign>('GET', `/api/v1/campaigns/${id}`);
  }

  async listCampaigns(page = 1, pageSize = 20): Promise<PaginatedResponse<Campaign>> {
    return this.request<PaginatedResponse<Campaign>>('GET', `/api/v1/campaigns?page=${page}&pageSize=${pageSize}`);
  }

  async getCampaignProgress(id: string): Promise<CampaignProgress> {
    return this.request<CampaignProgress>('GET', `/api/v1/campaigns/${id}/progress`);
  }

  async addCampaignRecipients(id: string, emails: string[]): Promise<{ added: number; skipped: number }> {
    return this.request<{ added: number; skipped: number }>('POST', `/api/v1/campaigns/${id}/recipients`, { emails });
  }

  async sendCampaign(id: string): Promise<{ sent: boolean }> {
    return this.request<{ sent: boolean }>('POST', `/api/v1/campaigns/${id}/send`);
  }

  async scheduleCampaign(id: string, scheduledAt: string): Promise<{ scheduled: boolean }> {
    return this.request<{ scheduled: boolean }>('POST', `/api/v1/campaigns/${id}/schedule`, { scheduledAt });
  }

  async pauseCampaign(id: string): Promise<{ paused: boolean }> {
    return this.request<{ paused: boolean }>('POST', `/api/v1/campaigns/${id}/pause`);
  }

  async resumeCampaign(id: string): Promise<{ resumed: boolean }> {
    return this.request<{ resumed: boolean }>('POST', `/api/v1/campaigns/${id}/resume`);
  }

  async deleteCampaign(id: string): Promise<{ deleted: boolean }> {
    return this.request<{ deleted: boolean }>('DELETE', `/api/v1/campaigns/${id}`);
  }
}
