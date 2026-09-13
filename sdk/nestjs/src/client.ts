import { Injectable, Global, Module, DynamicModule, Logger } from '@nestjs/common';

export interface NotificationHubConfig {
  apiKey: string;
  baseUrl?: string;
  timeout?: number;
}

export interface SendParams {
  recipientEmail: string;
  type: 'transactional' | 'marketing' | 'system';
  channel: 'email' | 'sms' | 'push' | 'webhook';
  payload: string | Record<string, unknown>;
  scheduledAt?: string;
  idempotencyKey?: string;
}

export interface SendResult {
  publicId: string;
}

export interface NotificationDetail {
  publicId: string;
  recipientEmail: string;
  type: string;
  channel: string;
  status: string;
  createdAt: string;
  processedAt?: string;
  provider?: string;
  providerMessageId?: string;
  lastError?: string;
  retryCount: number;
}

export interface PaginatedResult<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
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

@Injectable()
export class NotificationHubService {
  private readonly logger = new Logger(NotificationHubService.name);
  private readonly apiKey: string;
  private readonly baseUrl: string;
  private readonly timeout: number;

  constructor(config: NotificationHubConfig) {
    if (!config.apiKey) {
      throw new Error('API key is required. Get one at https://notificationhub.space/settings');
    }
    this.apiKey = config.apiKey;
    this.baseUrl = (config.baseUrl || 'https://api.notificationhub.space').replace(/\/$/, '');
    this.timeout = config.timeout || 30000;
  }

  /** Send a notification. */
  async send(params: SendParams): Promise<SendResult> {
    return this.request<SendResult>('POST', '/api/v1/notifications', params);
  }

  /** Get notification detail. */
  async getNotification(publicId: string): Promise<NotificationDetail> {
    return this.request<NotificationDetail>('GET', `/api/v1/notifications/${publicId}`);
  }

  /** List notifications with pagination. */
  async listNotifications(page = 1, pageSize = 20): Promise<PaginatedResult<NotificationDetail>> {
    return this.request<PaginatedResult<NotificationDetail>>(
      'GET',
      `/api/v1/notifications?page=${page}&pageSize=${pageSize}`,
    );
  }

  /** Retry a failed notification. */
  async retryNotification(publicId: string): Promise<void> {
    await this.request('POST', `/api/v1/notifications/${publicId}/retry`);
  }

  /** Create a template. */
  async createTemplate(name: string, subject: string, body: string): Promise<{ id: string; name: string }> {
    return this.request('POST', '/api/v1/templates', { name, subject, body });
  }

  /** Get campaign progress. */
  async getCampaignProgress(campaignId: string): Promise<CampaignProgress> {
    return this.request<CampaignProgress>('GET', `/api/v1/campaigns/${campaignId}/progress`);
  }

  /** Send a campaign immediately. */
  async sendCampaign(campaignId: string): Promise<void> {
    await this.request('POST', `/api/v1/campaigns/${campaignId}/send`);
  }

  private async request<T>(method: string, path: string, body?: unknown): Promise<T> {
    const url = `${this.baseUrl}${path}`;
    const controller = new AbortController();
    const timeoutId = setTimeout(() => controller.abort(), this.timeout);

    try {
      const response = await fetch(url, {
        method,
        headers: {
          'X-Api-Key': this.apiKey,
          'Content-Type': 'application/json',
        },
        body: body ? JSON.stringify(body) : undefined,
        signal: controller.signal,
      });

      clearTimeout(timeoutId);

      const text = await response.text();

      if (!response.ok) {
        this.logger.error(`NotificationHub API error ${response.status}: ${text}`);
        throw new Error(`NotificationHub API error ${response.status}: ${text}`);
      }

      return text ? JSON.parse(text) : ({} as T);
    } catch (error) {
      clearTimeout(timeoutId);
      if (error instanceof Error && error.name === 'AbortError') {
        throw new Error(`NotificationHub API request timed out after ${this.timeout}ms`);
      }
      throw error;
    }
  }
}

@Global()
@Module({})
export class NotificationHubModule {
  static forRoot(config: NotificationHubConfig): DynamicModule {
    return {
      module: NotificationHubModule,
      providers: [
        {
          provide: NotificationHubService,
          useFactory: () => new NotificationHubService(config),
        },
      ],
      exports: [NotificationHubService],
    };
  }

  static forRootAsync(useFactory: () => Promise<NotificationHubConfig> | NotificationHubConfig): DynamicModule {
    return {
      module: NotificationHubModule,
      providers: [
        {
          provide: NotificationHubService,
          useFactory: async () => {
            const config = await useFactory();
            return new NotificationHubService(config);
          },
        },
      ],
      exports: [NotificationHubService],
    };
  }
}
