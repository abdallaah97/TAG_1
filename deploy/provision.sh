#!/usr/bin/env bash
set -euo pipefail

for v in APP_ROOT APP_DIR BUILD_DIR SERVICE PORT ENV_FILE RUN_USER REDIS_DB \
         RATE_LIMIT DB_NAME MSSQL_CONTAINER MSSQL_PORT MSSQL_VOLUME \
         MSSQL_SA_PASSWORD JWT_KEY SEED_ADMIN_PASSWORD; do
  if [ -z "${!v:-}" ]; then echo "provision: missing required variable $v" >&2; exit 2; fi
done

command -v docker >/dev/null || { echo "provision: docker is not installed" >&2; exit 2; }

if ! command -v rsync >/dev/null; then
  echo "provision: installing rsync"
  DEBIAN_FRONTEND=noninteractive apt-get update -qq
  DEBIAN_FRONTEND=noninteractive apt-get install -y -qq rsync
fi

if ! id -u "$RUN_USER" >/dev/null 2>&1; then
  echo "provision: creating system user $RUN_USER"
  useradd --system --no-create-home --shell /usr/sbin/nologin "$RUN_USER"
fi

mkdir -p "$APP_DIR" "$BUILD_DIR"
chown -R "$RUN_USER":"$RUN_USER" "$APP_ROOT"

if docker ps -a --format '{{.Names}}' | grep -qx "$MSSQL_CONTAINER"; then
  docker start "$MSSQL_CONTAINER" >/dev/null 2>&1 || true
  echo "provision: container $MSSQL_CONTAINER already present"
else
  echo "provision: creating container $MSSQL_CONTAINER"
  docker volume create "$MSSQL_VOLUME" >/dev/null
  docker run -d --name "$MSSQL_CONTAINER" \
    --restart unless-stopped \
    --memory 4g --cpus 2 \
    -p "127.0.0.1:${MSSQL_PORT}:1433" \
    -e ACCEPT_EULA=Y \
    -e MSSQL_PID=Developer \
    -e "MSSQL_SA_PASSWORD=${MSSQL_SA_PASSWORD}" \
    -v "${MSSQL_VOLUME}:/var/opt/mssql" \
    mcr.microsoft.com/mssql/server:2022-latest >/dev/null
fi

for i in $(seq 1 40); do
  if docker exec "$MSSQL_CONTAINER" /opt/mssql-tools18/bin/sqlcmd \
       -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C -Q "SELECT 1" >/dev/null 2>&1; then
    echo "provision: sql server ready"
    break
  fi
  if [ "$i" -eq 40 ]; then
    echo "provision: sql server never became ready" >&2
    docker logs --tail 40 "$MSSQL_CONTAINER" >&2
    exit 1
  fi
  sleep 5
done

umask 077
cat > "$ENV_FILE" <<EOF
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://0.0.0.0:${PORT}
DOTNET_PRINT_TELEMETRY_MESSAGE=false
ConnectionStrings__Default=Server=127.0.0.1,${MSSQL_PORT};Database=${DB_NAME};User Id=sa;Password=${MSSQL_SA_PASSWORD};TrustServerCertificate=True;
ConnectionStrings__Redis=localhost:6379,defaultDatabase=${REDIS_DB}
Jwt__Key=${JWT_KEY}
Seed__SuperAdmin__Password=${SEED_ADMIN_PASSWORD}
RateLimit__PermitLimit=${RATE_LIMIT}
EOF
chmod 600 "$ENV_FILE"

cat > "/etc/systemd/system/${SERVICE}.service" <<EOF
[Unit]
Description=${SERVICE}
After=network.target docker.service
Wants=docker.service

[Service]
WorkingDirectory=${APP_DIR}
ExecStart=${APP_DIR}/API
Restart=always
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=${SERVICE}
User=${RUN_USER}
EnvironmentFile=${ENV_FILE}

[Install]
WantedBy=multi-user.target
EOF

systemctl daemon-reload
systemctl enable "$SERVICE" >/dev/null 2>&1
echo "provision: done"
