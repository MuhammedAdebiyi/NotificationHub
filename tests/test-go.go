// Test NotificationHub Go SDK
// Usage: NH_API_KEY=nhub_live_xxx go run tests/test-go.go

package main

import (
	"bytes"
	"encoding/json"
	"fmt"
	"io"
	"net/http"
	"os"
	"time"
)

var (
	apiKey  = os.Getenv("NH_API_KEY")
	baseURL = getEnv("NH_BASE_URL", "https://api.notificationhub.space")
	email   = getEnv("TEST_EMAIL", "test@notificationhub.space")
)

func getEnv(key, fallback string) string {
	if v := os.Getenv(key); v != "" {
		return v
	}
	return fallback
}

func api(method, path string, body interface{}) (map[string]interface{}, error) {
	var bodyReader io.Reader
	if body != nil {
		data, _ := json.Marshal(body)
		bodyReader = bytes.NewReader(data)
	}

	req, _ := http.NewRequest(method, baseURL+path, bodyReader)
	req.Header.Set("X-Api-Key", apiKey)
	req.Header.Set("Content-Type", "application/json")

	resp, err := http.DefaultClient.Do(req)
	if err != nil {
		return nil, fmt.Errorf("request failed: %w", err)
	}
	defer resp.Body.Close()

	respBody, _ := io.ReadAll(resp.Body)
	if resp.StatusCode >= 400 {
		return nil, fmt.Errorf("API %d: %s", resp.StatusCode, string(respBody))
	}

	var result map[string]interface{}
	json.Unmarshal(respBody, &result)
	return result, nil
}

func main() {
	if apiKey == "" {
		fmt.Println("Set NH_API_KEY env var")
		os.Exit(1)
	}

	fmt.Println("=== NotificationHub Go SDK Test ===\n")

	// 1. Send
	fmt.Println("1) Sending notification...")
	result, err := api("POST", "/api/v1/notifications", map[string]interface{}{
		"recipientEmail": email,
		"type":           "transactional",
		"channel":        "email",
		"payload": map[string]string{
			"subject": fmt.Sprintf("Test from Go SDK - %s", time.Now().Format("15:04:05")),
			"html":    "<h1>Hello!</h1><p>This is a test from the Go SDK.</p>",
		},
	})
	if err != nil {
		fmt.Printf("   ✗ Failed: %v\n", err)
		os.Exit(1)
	}
	publicID := result["publicId"].(string)
	fmt.Printf("   ✓ Sent! ID: %s\n", publicID)

	// 2. Get detail
	fmt.Println("\n2) Getting notification detail...")
	time.Sleep(2 * time.Second)
	detail, err := api("GET", "/api/v1/notifications/"+publicID, nil)
	if err != nil {
		fmt.Printf("   ✗ Failed: %v\n", err)
		os.Exit(1)
	}
	fmt.Printf("   ✓ Status: %v | Provider: %v\n", detail["status"], detail["provider"])

	// 3. List
	fmt.Println("\n3) Listing recent notifications...")
	list, err := api("GET", "/api/v1/notifications?page=1&pageSize=3", nil)
	if err != nil {
		fmt.Printf("   ✗ Failed: %v\n", err)
		os.Exit(1)
	}
	fmt.Printf("   ✓ Total: %v notifications\n", list["totalCount"])
	if items, ok := list["items"].([]interface{}); ok {
		for i, item := range items {
			if i >= 3 {
				break
			}
			n := item.(map[string]interface{})
			pid := fmt.Sprintf("%v", n["publicId"])
			if len(pid) > 12 {
				pid = pid[:12]
			}
			fmt.Printf("   - %s... | %v | %v\n", pid, n["status"], n["recipientEmail"])
		}
	}

	fmt.Println("\n=== All tests passed ===")
}
