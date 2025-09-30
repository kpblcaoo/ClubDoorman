#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
ENV_FILE="${ROOT_DIR}/.env"
ENV_TEMPLATE="${ROOT_DIR}/ClubDoorman/env.example"
SCHEMA_FILE="${ROOT_DIR}/scripts/init_clickhouse.sql"

log() {
    printf '[init-clickhouse] %s\n' "$*"
}

fatal() {
    printf '[init-clickhouse] ERROR: %s\n' "$*" >&2
    exit 1
}

ensure_env_file() {
    if [[ ! -f "${ENV_FILE}" ]]; then
        if [[ ! -f "${ENV_TEMPLATE}" ]]; then
            fatal "Cannot find env template at ${ENV_TEMPLATE}"
        fi
        log "Creating .env from template"
        cp "${ENV_TEMPLATE}" "${ENV_FILE}"
    else
        create_env_backup
    fi
}

create_env_backup() {
    local timestamp backup
    timestamp="$(date +%Y%m%d-%H%M%S)"
    backup="${ENV_FILE}.bak-${timestamp}"
    cp "${ENV_FILE}" "${backup}"
    log "Created .env backup at ${backup}"
}

read_env_var() {
    local key="$1"
    if [[ -f "${ENV_FILE}" ]]; then
        grep -m1 "^${key}=" "${ENV_FILE}" | cut -d'=' -f2- || true
    fi
}

update_env_var() {
    local key="$1"
    local value="$2"
    if grep -q "^${key}=" "${ENV_FILE}"; then
        # Use | as delimiter to avoid escaping URLs
        sed -i "s|^${key}=.*|${key}=${value}|" "${ENV_FILE}"
    else
        printf '%s=%s\n' "${key}" "${value}" >>"${ENV_FILE}"
    fi
}

ensure_env_default() {
    local key="$1"
    local default="$2"
    local current
    current="$(read_env_var "${key}")"
    if [[ -z "${current}" ]]; then
        update_env_var "${key}" "${default}"
        current="${default}"
        log "Added ${key}=${default} to .env"
    fi
    printf '%s' "${current}"
}

generate_password() {
    tr -dc 'A-Za-z0-9' </dev/urandom | head -c 24
}

ensure_env_file

# Ensure base values
CLICKHOUSE_ENABLED_CURRENT="$(read_env_var "DOORMAN_CLICKHOUSE__ENABLED")"
if [[ -z "${CLICKHOUSE_ENABLED_CURRENT}" ]]; then
    update_env_var "DOORMAN_CLICKHOUSE__ENABLED" "true"
    CLICKHOUSE_ENABLED_CURRENT="true"
    log "Added DOORMAN_CLICKHOUSE__ENABLED=true"
else
    log "Keeping existing DOORMAN_CLICKHOUSE__ENABLED=${CLICKHOUSE_ENABLED_CURRENT}"
fi
CLICKHOUSE_URL="$(ensure_env_default "DOORMAN_CLICKHOUSE__URL" "http://clickhouse:8123")"
CLICKHOUSE_DB="$(ensure_env_default "DOORMAN_CLICKHOUSE__DATABASE" "tg")"
CLICKHOUSE_TABLE="$(ensure_env_default "DOORMAN_CLICKHOUSE__RAW_TABLE" "tg.messages_raw")"
ensure_env_default "DOORMAN_CLICKHOUSE__INGEST_SOURCE" "local"
CLICKHOUSE_USER="$(ensure_env_default "DOORMAN_CLICKHOUSE__USERNAME" "doorman")"

if [[ ! "${CLICKHOUSE_USER}" =~ ^[a-zA-Z0-9_]+$ ]]; then
    fatal "DOORMAN_CLICKHOUSE__USERNAME must contain only letters, numbers or underscores"
fi

CLICKHOUSE_PASS="$(read_env_var "DOORMAN_CLICKHOUSE__PASSWORD")"
if [[ -z "${CLICKHOUSE_PASS}" ]]; then
    CLICKHOUSE_PASS="$(generate_password)"
    update_env_var "DOORMAN_CLICKHOUSE__PASSWORD" "${CLICKHOUSE_PASS}"
    log "Generated random DOORMAN_CLICKHOUSE__PASSWORD"
fi

if [[ ! -f "${SCHEMA_FILE}" ]]; then
    fatal "Schema file not found at ${SCHEMA_FILE}"
fi

if ! command -v docker >/dev/null 2>&1; then
    fatal "docker is required but not found in PATH"
fi

if ! docker compose version >/dev/null 2>&1; then
    fatal "docker compose command is required"
fi

log "Starting ClickHouse container (if not running)"
(cd "${ROOT_DIR}" && docker compose up -d clickhouse)

log "Waiting for ClickHouse to become available"
for attempt in $(seq 1 30); do
    if (cd "${ROOT_DIR}" && docker compose exec -T clickhouse clickhouse-client -q "SELECT 1") >/dev/null 2>&1; then
        break
    fi
    if [[ "${attempt}" -eq 30 ]]; then
        fatal "ClickHouse did not become ready in time"
    fi
    sleep 1
done

log "Applying base schema from scripts/init_clickhouse.sql"
(cd "${ROOT_DIR}" && docker compose exec -T clickhouse clickhouse-client --multiquery) <"${SCHEMA_FILE}"

log "Ensuring analytics user '${CLICKHOUSE_USER}' exists"
(cd "${ROOT_DIR}" && docker compose exec -T clickhouse clickhouse-client --multiquery <<SQL
CREATE USER IF NOT EXISTS ${CLICKHOUSE_USER} IDENTIFIED WITH plaintext_password BY '${CLICKHOUSE_PASS}';
ALTER USER ${CLICKHOUSE_USER} IDENTIFIED WITH plaintext_password BY '${CLICKHOUSE_PASS}';
GRANT SELECT, INSERT ON ${CLICKHOUSE_DB}.* TO ${CLICKHOUSE_USER};
SQL
)

log "ClickHouse initialization complete"
cat <<EOF
[init-clickhouse] Connection details written to .env:
  DOORMAN_CLICKHOUSE__URL=${CLICKHOUSE_URL}
  DOORMAN_CLICKHOUSE__DATABASE=${CLICKHOUSE_DB}
  DOORMAN_CLICKHOUSE__RAW_TABLE=${CLICKHOUSE_TABLE}
  DOORMAN_CLICKHOUSE__USERNAME=${CLICKHOUSE_USER}
  DOORMAN_CLICKHOUSE__PASSWORD=${CLICKHOUSE_PASS}
EOF
