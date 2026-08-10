#!/bin/bash
set -euo pipefail

STATE_FILE="/opt/notificationhub/active_color"
CURRENT=$(cat "$STATE_FILE" 2>/dev/null || echo "blue")
NEXT=$([ "$CURRENT" = "blue" ] && echo "green" || echo "blue")

API_PORT=$([ "$NEXT" = "blue" ] && echo "8091" || echo "8092")
IMAGE_API="ghcr.io/muhammedadebiyi/notificationhub/notificationhub-api:${IMAGE_TAG}"
IMAGE_WORKER="ghcr.io/muhammedadebiyi/notificationhub/notificationhub-worker:${IMAGE_TAG}"

echo "Current: $CURRENT → Deploying to: $NEXT"

docker pull "$IMAGE_API"
docker pull "$IMAGE_WORKER"

docker rm -f "notificationhub-api-$NEXT" 2>/dev/null || true
docker run -d --name "notificationhub-api-$NEXT" \
  --env-file /opt/notificationhub/.env \
  -p "${API_PORT}:8080" \
  --restart unless-stopped \
  --network notificationhub-net \
  "$IMAGE_API"

echo "Waiting for health check..."
for i in {1..30}; do
  if curl -sf "http://localhost:${API_PORT}/health" > /dev/null 2>&1; then
    echo "Health check passed."
    break
  fi
  if [ "$i" -eq 30 ]; then
    echo "Health check FAILED after 30 attempts. Rolling back — leaving $CURRENT active."
    docker rm -f "notificationhub-api-$NEXT"
    exit 1
  fi
  sleep 2
done

sed -i "s/notificationhub-api-$CURRENT/notificationhub-api-$NEXT/" /root/leadforge/caddy/Caddyfile
if ! docker exec leadforge-caddy caddy reload --config /etc/caddy/Caddyfile 2>&1; then
  echo "caddy reload failed — falling back to full recreate"
  cd /root/leadforge && docker compose up -d --no-deps --force-recreate caddy
fi

sleep 2
if ! curl -sf "https://169.58.108.115.sslip.io/health" > /dev/null 2>&1; then
  echo "WARNING: public health check failed after flip. NOT tearing down $CURRENT — investigate before retrying."
  echo "$NEXT" > "$STATE_FILE"
  exit 1
fi

docker rm -f "notificationhub-api-$CURRENT" 2>/dev/null || true

docker rm -f notificationhub-worker 2>/dev/null || true
docker run -d --name notificationhub-worker \
  --env-file /opt/notificationhub/.env \
  --restart unless-stopped \
  --network notificationhub-net \
  "$IMAGE_WORKER"

echo "$NEXT" > "$STATE_FILE"
echo "Deploy complete. Active color is now: $NEXT"
