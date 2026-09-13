<?php

declare(strict_types=1);

namespace NotificationHub\SDK;

class Client
{
    private string $apiKey;
    private string $baseUrl;
    private float $timeout;

    public function __construct(string $apiKey, string $baseUrl = 'https://api.notificationhub.space', float $timeout = 30.0)
    {
        if (empty($apiKey)) {
            throw new \InvalidArgumentException('API key is required. Get one at https://notificationhub.space/settings');
        }

        $this->apiKey = $apiKey;
        $this->baseUrl = rtrim($baseUrl, '/');
        $this->timeout = $timeout;
    }

    /**
     * Send a notification.
     *
     * @param array{recipientEmail: string, type: string, channel: string, payload: string|array} $params
     * @return array{publicId: string}
     */
    public function send(array $params): array
    {
        return $this->request('POST', '/api/v1/notifications', $params);
    }

    /**
     * Get notification detail.
     */
    public function getNotification(string $publicId): array
    {
        return $this->request('GET', "/api/v1/notifications/{$publicId}");
    }

    /**
     * List notifications with pagination.
     */
    public function listNotifications(int $page = 1, int $pageSize = 20): array
    {
        return $this->request('GET', "/api/v1/notifications?page={$page}&pageSize={$pageSize}");
    }

    /**
     * Retry a failed notification.
     */
    public function retryNotification(string $publicId): array
    {
        return $this->request('POST', "/api/v1/notifications/{$publicId}/retry");
    }

    /**
     * Create a template.
     */
    public function createTemplate(string $name, string $subject, string $body): array
    {
        return $this->request('POST', '/api/v1/templates', compact('name', 'subject', 'body'));
    }

    /**
     * Get a template by ID.
     */
    public function getTemplate(string $id): array
    {
        return $this->request('GET', "/api/v1/templates/{$id}");
    }

    /**
     * List templates.
     */
    public function listTemplates(int $page = 1, int $pageSize = 20): array
    {
        return $this->request('GET', "/api/v1/templates?page={$page}&pageSize={$pageSize}");
    }

    /**
     * Delete a template.
     */
    public function deleteTemplate(string $id): array
    {
        return $this->request('DELETE', "/api/v1/templates/{$id}");
    }

    /**
     * Create a campaign.
     */
    public function createCampaign(string $title, string $subject, ?string $body = null): array
    {
        $params = compact('title', 'subject');
        if ($body !== null) {
            $params['body'] = $body;
        }
        return $this->request('POST', '/api/v1/campaigns', $params);
    }

    /**
     * Add recipients to a campaign.
     */
    public function addCampaignRecipients(string $campaignId, array $emails): array
    {
        return $this->request('POST', "/api/v1/campaigns/{$campaignId}/recipients", ['emails' => $emails]);
    }

    /**
     * Send a campaign immediately.
     */
    public function sendCampaign(string $campaignId): array
    {
        return $this->request('POST', "/api/v1/campaigns/{$campaignId}/send");
    }

    /**
     * Get campaign progress.
     */
    public function getCampaignProgress(string $campaignId): array
    {
        return $this->request('GET', "/api/v1/campaigns/{$campaignId}/progress");
    }

    private function request(string $method, string $path, ?array $body = null): array
    {
        $url = $this->baseUrl . $path;

        $ch = curl_init();
        curl_setopt_array($ch, [
            CURLOPT_URL            => $url,
            CURLOPT_RETURNTRANSFER => true,
            CURLOPT_TIMEOUT        => $this->timeout,
            CURLOPT_HTTPHEADER     => [
                'X-Api-Key: ' . $this->apiKey,
                'Content-Type: application/json',
            ],
        ]);

        if ($method === 'POST') {
            curl_setopt($ch, CURLOPT_POST, true);
            if ($body !== null) {
                curl_setopt($ch, CURLOPT_POSTFIELDS, json_encode($body));
            }
        } elseif ($method === 'PUT') {
            curl_setopt($ch, CURLOPT_CUSTOMREQUEST, 'PUT');
            if ($body !== null) {
                curl_setopt($ch, CURLOPT_POSTFIELDS, json_encode($body));
            }
        } elseif ($method === 'DELETE') {
            curl_setopt($ch, CURLOPT_CUSTOMREQUEST, 'DELETE');
        }

        $response = curl_exec($ch);
        $statusCode = curl_getinfo($ch, CURLINFO_HTTP_CODE);
        $error = curl_error($ch);
        curl_close($ch);

        if ($response === false) {
            throw new NotificationHubException("cURL error: {$error}", 0);
        }

        $data = json_decode($response, true);

        if ($statusCode >= 400) {
            $message = $data['error'] ?? "HTTP {$statusCode}";
            throw new NotificationHubException($message, $statusCode);
        }

        return $data ?? [];
    }
}
