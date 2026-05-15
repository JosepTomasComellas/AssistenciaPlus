#!/bin/bash
# Actualitza AssistenciaPlus al servidor
# Ús: bash /docker/AssistenciaPlus/redeploy.sh
set -euo pipefail

DEPLOY_PATH="/docker/AssistenciaPlus"
cd "$DEPLOY_PATH"

echo "→ Obtenint canvis..."
git pull origin main

echo "→ Reconstruint i reiniciant serveis..."
docker compose up -d --build api web

echo "→ Netejant imatges antigues..."
docker image prune -f

echo ""
docker compose ps --format "table {{.Name}}\t{{.Status}}"
echo ""
echo "✓ Desplegament completat — $(git log -1 --format='%h %s')"
