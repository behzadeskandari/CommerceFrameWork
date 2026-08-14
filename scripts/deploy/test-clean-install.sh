#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
COMPOSE_FILE="${COMPOSE_FILE:-$ROOT/deploy/docker/docker-compose.yml}"
ENV_FILE="${ENV_FILE:-$ROOT/deploy/docker/.env}"
TIMEOUT_SECONDS="${TIMEOUT_SECONDS:-600}"

if [[ ! -f "$ENV_FILE" ]]; then
  cp "$ROOT/deploy/docker/.env.example" "$ENV_FILE"
  echo "Created $ENV_FILE from .env.example"
fi

set -a
# shellcheck disable=SC1090
source "$ENV_FILE"
set +a

BASE_URL="${COMMERCE_BASE_URL:-http://localhost:8080}"

wait_http() {
  local url="$1"
  local deadline=$((SECONDS + TIMEOUT_SECONDS))
  until curl -fsS "$url" >/dev/null 2>&1; do
    if (( SECONDS >= deadline )); then
      echo "Timed out waiting for $url" >&2
      return 1
    fi
    sleep 3
  done
}

cd "$ROOT"

echo "Tearing down previous stack..."
docker compose -f "$COMPOSE_FILE" --env-file "$ENV_FILE" down -v --remove-orphans

echo "Building and starting stack..."
docker compose -f "$COMPOSE_FILE" --env-file "$ENV_FILE" up -d --build

echo "Waiting for liveness at $BASE_URL/health/live ..."
wait_http "$BASE_URL/health/live"

: "${MSSQL_SA_PASSWORD:?Set MSSQL_SA_PASSWORD in .env}"
: "${COMMERCE_ADMIN_EMAIL:?Set COMMERCE_ADMIN_EMAIL in .env}"
: "${COMMERCE_ADMIN_USERNAME:?Set COMMERCE_ADMIN_USERNAME in .env}"
: "${COMMERCE_ADMIN_PASSWORD:?Set COMMERCE_ADMIN_PASSWORD in .env}"
: "${COMMERCE_STORE_NAME:?Set COMMERCE_STORE_NAME in .env}"
: "${COMMERCE_STORE_HOST:?Set COMMERCE_STORE_HOST in .env}"

CONNECTION_STRING="Server=sqlserver,1433;Database=Commerce;User Id=sa;Password=${MSSQL_SA_PASSWORD};TrustServerCertificate=True;Encrypt=True;"

echo "Running installation bootstrap..."
curl -fsS -X POST "$BASE_URL/installation/requirements" >/dev/null
curl -fsS -X POST "$BASE_URL/installation/database" \
  -H "Content-Type: application/json" \
  -d "{\"provider\":\"SqlServer\",\"connectionString\":\"${CONNECTION_STRING}\"}" >/dev/null
curl -fsS -X POST "$BASE_URL/installation/migrate" >/dev/null
curl -fsS -X POST "$BASE_URL/installation/seed" >/dev/null
curl -fsS -X POST "$BASE_URL/installation/admin" \
  -H "Content-Type: application/json" \
  -d "{\"email\":\"${COMMERCE_ADMIN_EMAIL}\",\"username\":\"${COMMERCE_ADMIN_USERNAME}\",\"password\":\"${COMMERCE_ADMIN_PASSWORD}\"}" >/dev/null
curl -fsS -X POST "$BASE_URL/installation/store" \
  -H "Content-Type: application/json" \
  -d "{\"name\":\"${COMMERCE_STORE_NAME}\",\"url\":\"${BASE_URL}\",\"hosts\":\"${COMMERCE_STORE_HOST}\"}" >/dev/null
curl -fsS -X POST "$BASE_URL/installation/language" \
  -H "Content-Type: application/json" \
  -d '{"name":"English","culture":"en-US","rtl":false,"isDefault":true}' >/dev/null
curl -fsS -X POST "$BASE_URL/installation/currency" \
  -H "Content-Type: application/json" \
  -d '{"name":"US Dollar","currencyCode":"USD","rate":1,"isPrimary":true}' >/dev/null
curl -fsS -X POST "$BASE_URL/installation/complete" >/dev/null

echo "Verifying readiness..."
READY_JSON="$(curl -fsS "$BASE_URL/health/ready")"
echo "$READY_JSON" | grep -q '"status":"Healthy"' && echo "Clean installation succeeded. Status: Healthy" || echo "WARN: readiness not Healthy — $READY_JSON"

ROOT_JSON="$(curl -fsS "$BASE_URL")"
echo "$ROOT_JSON" | grep -q '"status":"installed"' || {
  echo "Expected installed status at / but got: $ROOT_JSON" >&2
  exit 1
}

echo "PASS — clean Docker installation verified."
