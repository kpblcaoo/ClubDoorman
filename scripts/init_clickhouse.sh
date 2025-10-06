#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
ENV_FILE="${ROOT_DIR}/.env"
SCHEMA_FILE="${ROOT_DIR}/scripts/init_clickhouse.sql"

declare -a optional_suggestions=()
declare -a required_missing=()

log() {
    printf '[init-clickhouse] %s\n' "$*"
}

fatal() {
    printf '[init-clickhouse] ERROR: %s\n' "$*" >&2
    exit 1
}

add_unique() {
    local entry="$1"
    local array_name="$2"
    local -n ref="${array_name}"

    for existing in "${ref[@]-}"; do
        if [[ "${existing}" == "${entry}" ]]; then
            return
        fi
    done

    ref+=("${entry}")
}

load_env_file() {
    if [[ -f "${ENV_FILE}" ]]; then
        log "Loading environment from .env"
        set +u
        set -o allexport
        # shellcheck source=/dev/null
        source "${ENV_FILE}"
        set +o allexport
        set -u
    else
        log ".env not found, relying on current shell environment"
    fi
}

get_optional_env() {
    local key="$1"
    local default="$2"
    local current="${!key-}"

    if [[ -z "${current}" ]]; then
        add_unique "${key}=${default}" optional_suggestions
        printf '%s' "${default}"
    else
        printf '%s' "${current}"
    fi
}

note_required_missing() {
    local entry="$1"
    add_unique "${entry}" required_missing
}

generate_password() {
    local pw=""
    local old_pipefail
    old_pipefail="$(set +o | grep pipefail)"
    set +o pipefail
    while [[ ${#pw} -lt 24 ]]; do
        pw=$(LC_ALL=C tr -dc 'A-Za-z0-9' </dev/urandom | head -c 24 || true)
    done
    eval "$old_pipefail"
    printf '%s' "${pw}"
}

load_env_file

# Collect optional defaults (suggest but don't mutate)
get_optional_env "DOORMAN_CLICKHOUSE__ENABLED" "true" >/dev/null
CLICKHOUSE_URL="$(get_optional_env "DOORMAN_CLICKHOUSE__URL" "http://clickhouse:8123")"
CLICKHOUSE_DB="$(get_optional_env "DOORMAN_CLICKHOUSE__DATABASE" "tg")"
CLICKHOUSE_TABLE="$(get_optional_env "DOORMAN_CLICKHOUSE__RAW_TABLE" "tg.messages_raw")"
get_optional_env "DOORMAN_CLICKHOUSE__INGEST_SOURCE" "local" >/dev/null
CLICKHOUSE_USER="$(get_optional_env "DOORMAN_CLICKHOUSE__USERNAME" "doorman")"

if [[ ! "${CLICKHOUSE_USER}" =~ ^[a-zA-Z0-9_]+$ ]]; then
    fatal "DOORMAN_CLICKHOUSE__USERNAME must contain only letters, numbers or underscores"
fi

CLICKHOUSE_PASS="${DOORMAN_CLICKHOUSE__PASSWORD-}"
if [[ -z "${CLICKHOUSE_PASS}" ]]; then
    suggested_pass="$(generate_password)"
    note_required_missing "DOORMAN_CLICKHOUSE__PASSWORD=${suggested_pass}"
fi

if (( ${#required_missing[@]} > 0 )); then
    log "Missing required environment values; no changes were made."
    printf '[init-clickhouse] Please add the following to your .env (or export them) and rerun:\n'
    for entry in "${required_missing[@]}"; do
        printf '  %s\n' "${entry}"
    done
    exit 1
fi

if [[ ! "${CLICKHOUSE_PASS}" =~ ^[A-Za-z0-9_-]+$ ]]; then
    fatal "DOORMAN_CLICKHOUSE__PASSWORD must contain only letters, numbers, underscores, or hyphens"
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
[init-clickhouse] ClickHouse connection details (no files were modified):
  DOORMAN_CLICKHOUSE__URL=${CLICKHOUSE_URL}
  DOORMAN_CLICKHOUSE__DATABASE=${CLICKHOUSE_DB}
  DOORMAN_CLICKHOUSE__RAW_TABLE=${CLICKHOUSE_TABLE}
  DOORMAN_CLICKHOUSE__USERNAME=${CLICKHOUSE_USER}
  DOORMAN_CLICKHOUSE__PASSWORD=${CLICKHOUSE_PASS}
EOF

if (( ${#optional_suggestions[@]} > 0 )); then
    printf '[init-clickhouse] Consider appending the following to your .env for future runs:\n'
    for entry in "${optional_suggestions[@]}"; do
        printf '  %s\n' "${entry}"
    done
fi
