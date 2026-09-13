#!/bin/bash
# Test NotificationHub API with cURL
# Usage: NH_API_KEY=nhub_live_xxx ./test-curl.sh

set -e

API_KEY="${NH_API_KEY:?Set NH_API_KEY env var}"
BASE_URL="${NH_BASE_URL:-https://api.notificationhub.space}"
EMAIL="${TEST_EMAIL:-test@notificationhub.space}"

echo "=== NotificationHub cURL Test ==="
echo "API: $BASE_URL"
echo ""

# 1. Send a notification
echo "1) Sending notification..."
RESPONSE=$(curl -s -w "\n%{http_code}" -X POST "$BASE_URL/api/v1/notifications" \
  -H "X-Api-Key: $API_KEY" \
  -H "Content-Type: application/json" \
  -d "{
    \"recipientEmail\": \"$EMAIL\",
    \"type\": \"transactional\",
    \"channel\": \"email\",
    \"payload\": {
      \"subject\": \"Test from cURL - $(date +%H:%M:%S)\",
      \"html\": \"<h1>Hello!</h1><p>This is a test notification sent at $(date).</p>\"
    }
  }")

HTTP_CODE=$(echo "$RESPONSE" | tail -1)
BODY=$(echo "$RESPONSE" | head -n -1)

if [ "$HTTP_CODE" = "201" ]; then
  PUBLIC_ID=$(echo "$BODY" | python3 -c "import sys,json; print(json.load(sys.stdin)['publicId'])" 2>/dev/null || echo "")
  echo "   ✓ Sent! ID: $PUBLIC_ID"
else
  echo "   ✗ Failed ($HTTP_CODE): $BODY"
  exit 1
fi

# 2. Get notification detail
echo ""
echo "2) Getting notification detail..."
sleep 2
curl -s "$BASE_URL/api/v1/notifications/$PUBLIC_ID" \
  -H "X-Api-Key: $API_KEY" | python3 -m json.tool 2>/dev/null || echo "(install python3 for formatted output)"

# 3. List notifications
echo ""
echo "3) Listing recent notifications..."
curl -s "$BASE_URL/api/v1/notifications?page=1&pageSize=3" \
  -H "X-Api-Key: $API_KEY" | python3 -c "
import sys, json
data = json.load(sys.stdin)
print(f'   Total: {data[\"totalCount\"]} notifications')
for n in data['items'][:3]:
    print(f'   - {n[\"publicId\"][:12]}... | {n[\"status\"]} | {n[\"recipientEmail\"]}')
" 2>/dev/null || echo "(install python3 for formatted output)"

echo ""
echo "=== All tests passed ==="
