#!/bin/bash
set -euo pipefail

STATE_FILE="/opt/notificationhub/active_color"
CURRENT=$(cat "$STATE_FILE" 2>/dev/null || echo "blue")
NEXT=$([ "$CURRENT" = "blue" ] && echo "green" || echo "blue")

API_PORT=$([ "$NEXT" = "blue" ] && echo "8091" || echo "8092")
IMAGE_API="ghcr.io/muhammedadebiyi/notificationhub/notificationhub-api:${IMAGE_TAG}"
IMAGE_WORKER="ghcr.io/muhammedadebiyi/notificationhub/notificationhub-worker:${IMAGE_TAG}"

echo "Current: $CURRENT → Deploying to: $NEXT"

pull_retry() {
  for i in 1 2 3 4 5; do
    if docker pull "$1"; then return 0; fi
    echo "pull failed for $1 — retry $i/5 in 15s"
    sleep 15
  done
  echo "FATAL: could not pull $1 after 5 attempts"
  return 1
}
pull_retry "$IMAGE_API" || exit 1
pull_retry "$IMAGE_WORKER" || exit 1

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
    echo "Health (liveness) check passed."
    if curl -sf "http://localhost:${API_PORT}/ready" > /dev/null 2>&1; then
      echo "Readiness check passed."
    else
      echo "WARNING: readiness check failed (database unreachable?) — proceeding; liveness only."
    fi
    break
  fi
  if [ "$i" -eq 30 ]; then
    echo "Health check FAILED after 30 attempts. Rolling back."
    docker rm -f "notificationhub-api-$NEXT"
    exit 1
  fi
  sleep 2
done

cat > /opt/notificationhub/caddy/Caddyfile << CADDYEOF
api.notificationhub.space {
    log
    reverse_proxy notificationhub-api-${NEXT}:8080
}

169.58.108.115.sslip.io {
    log
    reverse_proxy notificationhub-api-${NEXT}:8080
}

notificationhub.space {
    redir https://notification-hub-chi.vercel.app{uri} permanent
}
CADDYEOF

if ! docker exec notificationhub-caddy caddy reload --config /etc/caddy/Caddyfile 2>&1; then
  echo "caddy reload failed — restarting container"
  docker restart notificationhub-caddy
fi

sleep 2
if ! curl -sf "https://169.58.108.115.sslip.io/health" > /dev/null 2>&1; then
  echo "WARNING: public health check failed after flip. NOT tearing down $CURRENT."
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
