#!/bin/bash
# =============================================================
# deploy.sh  (Linux - servidor Ubuntu)
# Script de desplegament d'AssistenciaPlus
#
# Ús:
#   bash scripts/deploy.sh --full-deploy    # Primera instal·lació
#   bash scripts/deploy.sh --update         # Actualitzar (per defecte)
#   bash scripts/deploy.sh --restart        # Reiniciar contenidors
#   bash scripts/deploy.sh --logs           # Veure logs en directe
#   bash scripts/deploy.sh --status         # Estat dels contenidors
#   bash scripts/deploy.sh --backup         # Fer backup de la BD
#   bash scripts/deploy.sh --restore FILE   # Restaurar backup
# =============================================================

set -euo pipefail

# ── Colors ────────────────────────────────────────────────────
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
WHITE='\033[1;37m'
NC='\033[0m' # No Color

# ── Configuració ──────────────────────────────────────────────
DEPLOY_PATH="/docker/AssistenciaPlus"
BACKUP_PATH="/docker/backups/assistenciaplus"
LOG_FILE="/var/log/assistenciaplus-deploy.log"
GITHUB_REPO=""  # S'omple des del .env o com a paràmetre

# ── Helpers ───────────────────────────────────────────────────
log()  { echo -e "${CYAN}>> $1${NC}"; echo "[$(date '+%Y-%m-%d %H:%M:%S')] $1" >> "$LOG_FILE" 2>/dev/null || true; }
ok()   { echo -e "${GREEN}   OK: $1${NC}"; }
warn() { echo -e "${YELLOW}   WARN: $1${NC}"; }
fail() { echo -e "${RED}   ERROR: $1${NC}"; exit 1; }

print_header() {
    echo ""
    echo -e "${CYAN}============================================${NC}"
    echo -e "${CYAN}  AssistenciaPlus - Script de desplegament  ${NC}"
    echo -e "${CYAN}============================================${NC}"
    echo -e "  Data    : $(date '+%Y-%m-%d %H:%M:%S')"
    echo -e "  Ruta    : ${DEPLOY_PATH}"
    echo -e "  Mode    : ${WHITE}${MODE}${NC}"
    echo -e "${CYAN}============================================${NC}"
    echo ""
}

# ── Llegir mode ───────────────────────────────────────────────
MODE="update"
RESTORE_FILE=""

while [[ $# -gt 0 ]]; do
    case $1 in
        --full-deploy)  MODE="full-deploy" ;;
        --update)       MODE="update" ;;
        --restart)      MODE="restart" ;;
        --logs)         MODE="logs" ;;
        --status)       MODE="status" ;;
        --backup)       MODE="backup" ;;
        --restore)      MODE="restore"; RESTORE_FILE="${2:-}"; shift ;;
        --repo)         GITHUB_REPO="${2:-}"; shift ;;
        *) fail "Parametre desconegut: $1" ;;
    esac
    shift
done

print_header

# ── Verificar dependències ────────────────────────────────────
check_deps() {
    log "Verificant dependències..."
    command -v docker >/dev/null 2>&1 || fail "Docker no instal·lat. Executa: curl -fsSL https://get.docker.com | sh"
    command -v git    >/dev/null 2>&1 || fail "Git no instal·lat: sudo apt install git"
    ok "Docker $(docker --version | cut -d' ' -f3 | tr -d ',')"
    ok "Docker Compose $(docker compose version --short)"
    ok "Git $(git --version | cut -d' ' -f3)"
}

# ── Verificar .env ────────────────────────────────────────────
check_env() {
    if [[ ! -f "${DEPLOY_PATH}/.env" ]]; then
        fail ".env no trobat a ${DEPLOY_PATH}/.env\nCopia .env.example a .env i omple els valors."
    fi
    # Verificar camps obligatoris
    source "${DEPLOY_PATH}/.env"
    [[ -z "${DB_PASSWORD:-}" ]]    && fail "DB_PASSWORD no configurat al .env"
    [[ -z "${JWT_SECRET:-}" ]]     && fail "JWT_SECRET no configurat al .env"
    [[ -z "${REDIS_PASSWORD:-}" ]] && fail "REDIS_PASSWORD no configurat al .env"
    ok ".env verificat"
}

# ── Verificar certificats SSL ─────────────────────────────────
check_certs() {
    if [[ ! -f "${DEPLOY_PATH}/nginx/certs/fullchain.pem" ]] || \
       [[ ! -f "${DEPLOY_PATH}/nginx/certs/privkey.pem" ]]; then
        warn "Certificats SSL no trobats a nginx/certs/"
        warn "Afegeix fullchain.pem i privkey.pem abans d'iniciar nginx"
    else
        ok "Certificats SSL trobats"
    fi
}

# ═══════════════════════════════════════════════════════════════
# MODE: PRIMERA INSTAL·LACIÓ COMPLETA
# ═══════════════════════════════════════════════════════════════
do_full_deploy() {
    log "Iniciant instal·lació completa..."

    # Docker
    if ! command -v docker &>/dev/null; then
        log "Instal·lant Docker..."
        curl -fsSL https://get.docker.com -o /tmp/get-docker.sh
        sudo sh /tmp/get-docker.sh
        sudo usermod -aG docker "$USER"
        rm /tmp/get-docker.sh
        ok "Docker instal·lat. IMPORTANT: Tanca la sessió SSH i torna a entrar perquè el grup docker tingui efecte"
        ok "Despres torna a executar: bash scripts/deploy.sh --update"
        exit 0
    fi

    # Estructura de directoris
    log "Creant estructura de directoris..."
    sudo mkdir -p "${DEPLOY_PATH}"
    sudo mkdir -p "${BACKUP_PATH}"
    sudo chown -R "$USER:$USER" "${DEPLOY_PATH}"
    sudo chown -R "$USER:$USER" "${BACKUP_PATH}"
    ok "Directoris creats"

    # Clonar repositori
    if [[ -d "${DEPLOY_PATH}/.git" ]]; then
        warn "Repositori ja existeix, fent pull..."
        cd "${DEPLOY_PATH}"
        git pull origin main
    else
        if [[ -z "$GITHUB_REPO" ]]; then
            fail "Indica el repositori GitHub: bash scripts/deploy.sh --full-deploy --repo https://github.com/USUARI/AssistenciaPlus.git"
        fi
        log "Clonant repositori: ${GITHUB_REPO}..."
        git clone "$GITHUB_REPO" "${DEPLOY_PATH}"
        ok "Repositori clonat"
    fi

    cd "${DEPLOY_PATH}"
    check_env
    check_certs

    # Directori de certificats
    mkdir -p nginx/certs

    # Primer build i aixecar
    log "Construint imatges Docker (pot trigar uns minuts)..."
    docker compose build --no-cache
    ok "Imatges construïdes"

    log "Aixecant tots els serveis..."
    docker compose up -d
    ok "Serveis en marxa"

    # Esperar que l'API estigui sana
    log "Esperant que l'API apliqui les migracions de BD..."
    sleep 15
    for i in {1..12}; do
        if docker compose exec -T api wget -qO- http://localhost:8080/health &>/dev/null; then
            ok "API sana i BD migrada"
            break
        fi
        echo -n "."
        sleep 5
    done

    # Descarregar model Ollama
    log "Descarregant model d'IA Ollama (llama3.2, ~2GB)..."
    warn "Això pot trigar uns minuts depenent de la connexió..."
    source .env
    docker exec assistenciaplus_ollama ollama pull "${OLLAMA_MODEL:-llama3.2}" || \
        warn "No s'ha pogut descarregar el model Ollama. Pots fer-ho manualment: docker exec assistenciaplus_ollama ollama pull llama3.2"

    # Configurar backup automàtic
    setup_cron_backup

    echo ""
    echo -e "${GREEN}============================================${NC}"
    echo -e "${GREEN}  INSTAL·LACIÓ COMPLETADA!                 ${NC}"
    echo -e "${GREEN}============================================${NC}"

    source .env
    echo -e "  Aplicació: ${CYAN}${APP_PUBLIC_URL}${NC}"
    echo ""
    echo -e "${YELLOW}Proxim pas: Crea el primer usuari administrador${NC}"
    echo -e "  bash scripts/deploy.sh --create-admin"
    echo ""
    do_status
}

# ═══════════════════════════════════════════════════════════════
# MODE: ACTUALITZAR (el més habitual, cridat pel CI/CD)
# ═══════════════════════════════════════════════════════════════
do_update() {
    cd "${DEPLOY_PATH}"
    check_deps
    check_env

    log "Obtenint darrers canvis de GitHub..."
    git fetch origin main
    LOCAL=$(git rev-parse HEAD)
    REMOTE=$(git rev-parse origin/main)

    if [[ "$LOCAL" == "$REMOTE" ]]; then
        warn "No hi ha canvis nous. El servidor ja esta al dia."
        do_status
        exit 0
    fi

    log "Actualitzant codi ($(git log --oneline HEAD..origin/main | wc -l) commits nous)..."
    git pull origin main
    ok "Codi actualitzat"

    # Fer backup preventiu de la BD
    log "Backup preventiu de la base de dades..."
    do_backup "pre-deploy"

    log "Reconstruint i reiniciant serveis (zero downtime)..."
    docker compose pull 2>/dev/null || true
    docker compose up -d --build --no-deps api web
    ok "Serveis actualitzats"

    # Neteja d'imatges antigues
    log "Netejant imatges Docker antigues..."
    docker image prune -f >/dev/null
    ok "Neteja completada"

    # Verificar salut
    sleep 10
    log "Verificant salut dels serveis..."
    if docker compose exec -T api wget -qO- http://localhost:8080/health &>/dev/null; then
        ok "API sana"
    else
        fail "L'API no respon. Comprova els logs: bash scripts/deploy.sh --logs"
    fi

    echo ""
    echo -e "${GREEN}============================================${NC}"
    echo -e "${GREEN}  ACTUALITZACIO COMPLETADA!                 ${NC}"
    echo -e "${GREEN}============================================${NC}"
    echo ""
    do_status
}

# ═══════════════════════════════════════════════════════════════
# MODE: REINICI
# ═══════════════════════════════════════════════════════════════
do_restart() {
    cd "${DEPLOY_PATH}"
    log "Reiniciant tots els serveis..."
    docker compose restart
    ok "Serveis reiniciats"
    do_status
}

# ═══════════════════════════════════════════════════════════════
# MODE: ESTAT
# ═══════════════════════════════════════════════════════════════
do_status() {
    cd "${DEPLOY_PATH}"
    echo ""
    echo -e "${CYAN}── Estat dels contenidors ───────────────────${NC}"
    docker compose ps --format "table {{.Name}}\t{{.Status}}\t{{.Ports}}"
    echo ""

    # Ús de recursos
    echo -e "${CYAN}── Us de recursos ───────────────────────────${NC}"
    docker stats --no-stream --format "table {{.Name}}\t{{.CPUPerc}}\t{{.MemUsage}}" \
        assistenciaplus_api \
        assistenciaplus_db \
        assistenciaplus_redis \
        assistenciaplus_web \
        assistenciaplus_nginx 2>/dev/null || true
    echo ""

    # Versió desplegada
    if [[ -d ".git" ]]; then
        echo -e "${CYAN}── Versio desplegada ─────────────────────────${NC}"
        echo "  Commit : $(git log -1 --format='%h - %s (%ar)')"
        echo "  Branca : $(git branch --show-current)"
        echo ""
    fi
}

# ═══════════════════════════════════════════════════════════════
# MODE: LOGS
# ═══════════════════════════════════════════════════════════════
do_logs() {
    cd "${DEPLOY_PATH}"
    log "Mostrant logs en directe (Ctrl+C per sortir)..."
    docker compose logs -f --tail=50
}

# ═══════════════════════════════════════════════════════════════
# MODE: BACKUP
# ═══════════════════════════════════════════════════════════════
do_backup() {
    local SUFFIX="${1:-manual}"
    cd "${DEPLOY_PATH}"
    source .env

    mkdir -p "${BACKUP_PATH}"
    local FILENAME="db_$(date +%Y%m%d_%H%M%S)_${SUFFIX}.sql.gz"
    local FILEPATH="${BACKUP_PATH}/${FILENAME}"

    log "Fent backup de la base de dades..."
    docker exec assistenciaplus_db pg_dump \
        -U "${DB_USER}" "${DB_NAME}" \
        | gzip > "${FILEPATH}"

    local SIZE=$(du -sh "${FILEPATH}" | cut -f1)
    ok "Backup creat: ${FILEPATH} (${SIZE})"

    # Eliminar backups de més de 30 dies
    find "${BACKUP_PATH}" -name "db_*.sql.gz" -mtime +30 -delete 2>/dev/null || true
    ok "Backups antics netejats (>30 dies)"
}

# ═══════════════════════════════════════════════════════════════
# MODE: RESTAURAR BACKUP
# ═══════════════════════════════════════════════════════════════
do_restore() {
    if [[ -z "$RESTORE_FILE" ]] || [[ ! -f "$RESTORE_FILE" ]]; then
        fail "Indica un fitxer de backup vàlid:\n  bash scripts/deploy.sh --restore /docker/backups/assistenciaplus/db_20241201_020000.sql.gz"
    fi

    cd "${DEPLOY_PATH}"
    source .env

    warn "ATENCIÓ: Aquesta operació sobreescriurà la base de dades actual!"
    read -rp "Estàs segur? (escriu 'si' per confirmar): " CONFIRM
    [[ "$CONFIRM" != "si" ]] && { warn "Cancel·lat."; exit 0; }

    log "Fent backup previ de seguretat..."
    do_backup "pre-restore"

    log "Restaurant backup: ${RESTORE_FILE}..."
    gunzip -c "${RESTORE_FILE}" | \
        docker exec -i assistenciaplus_db psql -U "${DB_USER}" "${DB_NAME}"
    ok "Backup restaurat correctament"

    log "Reiniciant l'API..."
    docker compose restart api
    ok "API reiniciada"
}

# ── Configurar cron per backups automàtics ────────────────────
setup_cron_backup() {
    local CRON_JOB="0 2 * * * cd ${DEPLOY_PATH} && bash scripts/deploy.sh --backup >> /var/log/assistenciaplus-backup.log 2>&1"
    (crontab -l 2>/dev/null | grep -v "deploy.sh --backup"; echo "$CRON_JOB") | crontab -
    ok "Backup automàtic programat (cada dia a les 02:00)"
}

# ═══════════════════════════════════════════════════════════════
# EXECUTAR EL MODE SELECCIONAT
# ═══════════════════════════════════════════════════════════════
case "$MODE" in
    full-deploy) do_full_deploy ;;
    update)      do_update ;;
    restart)     do_restart ;;
    logs)        do_logs ;;
    status)      do_status ;;
    backup)      do_backup "manual" ;;
    restore)     do_restore ;;
esac
