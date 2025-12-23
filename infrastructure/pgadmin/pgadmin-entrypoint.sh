#!/bin/bash
set -e

DB_HOST="${PGADMIN_DEFAULT_SERVER_HOST:?ENV PGADMIN_DEFAULT_SERVER_HOST is required}"
DB_PORT="${PGADMIN_DEFAULT_SERVER_PORT:?ENV PGADMIN_DEFAULT_SERVER_PORT is required}"
DB_NAME="${AUTH_POSTGRES_DB:?ENV AUTH_POSTGRES_DB is required}"
DB_USER="${AUTH_POSTGRES_USER:?ENV AUTH_POSTGRES_USER is required}"
DB_PASSWORD="${AUTH_POSTGRES_PASSWORD:?ENV AUTH_POSTGRES_PASSWORD is required}"
SERVER_NAME="${PGADMIN_SERVER_NAME:?ENV PGADMIN_SERVER_NAME is required}"

SERVERS_JSON_PATH="/var/lib/pgadmin/servers.json"

echo ">>> pgAdmin bootstrap: хост ${DB_HOST}:${DB_PORT}, база ${DB_NAME}, юзер ${DB_USER}, сервер '${SERVER_NAME}'"

mkdir -p "$(dirname "${SERVERS_JSON_PATH}")"

cat > "${SERVERS_JSON_PATH}" <<EOF
{
  "Servers": {
    "1": {
      "Group": "Servers",
      "Name": "${SERVER_NAME}",
      "Host": "${DB_HOST}",
      "Port": ${DB_PORT},
      "MaintenanceDB": "${DB_NAME}",
      "Username": "${DB_USER}",
      "Password": "${DB_PASSWORD}",
      "SSLMode": "prefer",
      "Favorite": true
    }
  }
}
EOF

export PGADMIN_SERVER_JSON_FILE="${SERVERS_JSON_PATH}"
export PGADMIN_REPLACE_SERVERS_ON_STARTUP="True"

echo ">>> pgAdmin bootstrap: стартуем основной entrypoint"
exec /entrypoint.sh