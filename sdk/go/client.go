// Package nh provides an official Go SDK for NotificationHub.
//
// Usage:
//
//	client := nh.NewClient("nhub_live_your_key")
//	result, err := client.Send(ctx, &nh.SendParams{
//	    RecipientEmail: "user@example.com",
//	    Type:           "transactional",
//	    Channel:        "email",
//	    Payload:        map[string]interface{}{"subject": "Hi", "html": "<p>Hello</p>"},
//	})
package nh

import (
	"bytes"
	"context"
	"encoding/json"
	"fmt"
	"io"
	"net/http"
	"time"
)

const DefaultBaseURL = "https://api.notificationhub.space"

// Client is the NotificationHub API client.
type Client struct {
	apiKey     string
	baseURL    string
	httpClient *http.Client
}

// NewClient creates a new NotificationHub client.
func NewClient(apiKey string, opts ...Option) *Client {
	c := &Client{
		apiKey:  apiKey,
		baseURL: DefaultBaseURL,
		httpClient: &http.Client{
			Timeout: 30 * time.Second,
		},
	}
	for _, opt := range opts {
		opt(c)
	}
	return c
}

// Option configures the client.
type Option func(*Client)

// WithBaseURL overrides the default API base URL.
func WithBaseURL(url string) Option {
	return func(c *Client) { c.baseURL = url }
}

// WithHTTPClient overrides the default HTTP client.
func WithHTTPClient(hc *http.Client) Option {
	return func(c *Client) { c.httpClient = hc }
}

// SendParams defines the request body for sending a notification.
type SendParams struct {
	RecipientEmail string      `json:"recipientEmail"`
	Type           string      `json:"type"`
	Channel        string      `json:"channel"`
	Payload        interface{} `json:"payload"`
}

// SendResult is the response from sending a notification.
type SendResult struct {
	PublicID string `json:"publicId"`
}

// Notification represents a notification detail.
type Notification struct {
	PublicID          string  `json:"publicId"`
	RecipientEmail   string  `json:"recipientEmail"`
	Type             string  `json:"type"`
	Channel          string  `json:"channel"`
	Status           string  `json:"status"`
	Payload          string  `json:"payload"`
	CreatedAt        string  `json:"createdAt"`
	ProcessedAt      *string `json:"processedAt,omitempty"`
	Provider         *string `json:"provider,omitempty"`
	ProviderMessageID *string `json:"providerMessageId,omitempty"`
	LastError        *string `json:"lastError,omitempty"`
	RetryCount       int     `json:"retryCount"`
}

// PaginatedNotifications is a paginated list of notifications.
type PaginatedNotifications struct {
	Items      []Notification `json:"items"`
	TotalCount int            `json:"totalCount"`
	PageNumber int            `json:"pageNumber"`
	PageSize   int            `json:"pageSize"`
}

// Template represents an email template.
type Template struct {
	ID        string `json:"id"`
	Name      string `json:"name"`
	Subject   string `json:"subject"`
	Body      string `json:"body,omitempty"`
	CreatedAt string `json:"createdAt"`
}

// PaginatedTemplates is a paginated list of templates.
type PaginatedTemplates struct {
	Items      []Template `json:"items"`
	TotalCount int        `json:"totalCount"`
	PageNumber int        `json:"pageNumber"`
	PageSize   int        `json:"pageSize"`
}

// CampaignProgress represents real-time campaign progress.
type CampaignProgress struct {
	TotalRecipients int     `json:"totalRecipients"`
	Sent            int     `json:"sent"`
	Pending         int     `json:"pending"`
	Processing      int     `json:"processing"`
	Retrying        int     `json:"retrying"`
	Failed          int     `json:"failed"`
	DeadLetter      int     `json:"deadLetter"`
	ProgressPercent float64 `json:"progressPercent"`
}

// Send sends a notification.
func (c *Client) Send(ctx context.Context, params *SendParams) (*SendResult, error) {
	var result SendResult
	if err := c.do(ctx, http.MethodPost, "/api/v1/notifications", params, &result); err != nil {
		return nil, err
	}
	return &result, nil
}

// GetNotification retrieves a notification by public ID.
func (c *Client) GetNotification(ctx context.Context, publicID string) (*Notification, error) {
	var result Notification
	if err := c.do(ctx, http.MethodGet, "/api/v1/notifications/"+publicID, nil, &result); err != nil {
		return nil, err
	}
	return &result, nil
}

// ListNotifications lists notifications with optional filters.
func (c *Client) ListNotifications(ctx context.Context, page, pageSize int) (*PaginatedNotifications, error) {
	url := fmt.Sprintf("/api/v1/notifications?page=%d&pageSize=%d", page, pageSize)
	var result PaginatedNotifications
	if err := c.do(ctx, http.MethodGet, url, nil, &result); err != nil {
		return nil, err
	}
	return &result, nil
}

// RetryNotification retries a failed notification.
func (c *Client) RetryNotification(ctx context.Context, publicID string) error {
	return c.do(ctx, http.MethodPost, "/api/v1/notifications/"+publicID+"/retry", nil, nil)
}

// CreateTemplate creates a new email template.
func (c *Client) CreateTemplate(ctx context.Context, name, subject, body string) (*Template, error) {
	payload := map[string]string{"name": name, "subject": subject, "body": body}
	var result Template
	if err := c.do(ctx, http.MethodPost, "/api/v1/templates", payload, &result); err != nil {
		return nil, err
	}
	return &result, nil
}

// GetTemplate retrieves a template by ID.
func (c *Client) GetTemplate(ctx context.Context, id string) (*Template, error) {
	var result Template
	if err := c.do(ctx, http.MethodGet, "/api/v1/templates/"+id, nil, &result); err != nil {
		return nil, err
	}
	return &result, nil
}

// ListTemplates lists all templates.
func (c *Client) ListTemplates(ctx context.Context, page, pageSize int) (*PaginatedTemplates, error) {
	url := fmt.Sprintf("/api/v1/templates?page=%d&pageSize=%d", page, pageSize)
	var result PaginatedTemplates
	if err := c.do(ctx, http.MethodGet, url, nil, &result); err != nil {
		return nil, err
	}
	return &result, nil
}

// DeleteTemplate deletes a template by ID.
func (c *Client) DeleteTemplate(ctx context.Context, id string) error {
	return c.do(ctx, http.MethodDelete, "/api/v1/templates/"+id, nil, nil)
}

// GetCampaignProgress gets real-time campaign progress.
func (c *Client) GetCampaignProgress(ctx context.Context, campaignID string) (*CampaignProgress, error) {
	var result CampaignProgress
	if err := c.do(ctx, http.MethodGet, "/api/v1/campaigns/"+campaignID+"/progress", nil, &result); err != nil {
		return nil, err
	}
	return &result, nil
}

// SendCampaign sends a campaign immediately.
func (c *Client) SendCampaign(ctx context.Context, campaignID string) error {
	return c.do(ctx, http.MethodPost, "/api/v1/campaigns/"+campaignID+"/send", nil, nil)
}

func (c *Client) do(ctx context.Context, method, path string, body interface{}, result interface{}) error {
	var bodyReader io.Reader
	if body != nil {
		data, err := json.Marshal(body)
		if err != nil {
			return fmt.Errorf("marshal body: %w", err)
		}
		bodyReader = bytes.NewReader(data)
	}

	req, err := http.NewRequestWithContext(ctx, method, c.baseURL+path, bodyReader)
	if err != nil {
		return fmt.Errorf("create request: %w", err)
	}

	req.Header.Set("X-Api-Key", c.apiKey)
	req.Header.Set("Content-Type", "application/json")

	resp, err := c.httpClient.Do(req)
	if err != nil {
		return fmt.Errorf("execute request: %w", err)
	}
	defer resp.Body.Close()

	respBody, err := io.ReadAll(resp.Body)
	if err != nil {
		return fmt.Errorf("read response: %w", err)
	}

	if resp.StatusCode >= 400 {
		return fmt.Errorf("API error %d: %s", resp.StatusCode, string(respBody))
	}

	if result != nil {
		if err := json.Unmarshal(respBody, result); err != nil {
			return fmt.Errorf("unmarshal response: %w", err)
		}
	}

	return nil
}
