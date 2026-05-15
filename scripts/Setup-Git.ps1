# =============================================================
# Setup-Git.ps1  (Windows PowerShell)
# Inicialitza el repositori Git local i el puja a GitHub
#
# Ús:
#   cd C:\ruta\AssistenciaPlus
#   .\scripts\Setup-Git.ps1 -GitHubUser "el_teu_usuari"
#
# Opcional:
#   .\scripts\Setup-Git.ps1 -GitHubUser "el_teu_usuari" -RepoName "AssistenciaPlus" -GitEmail "tu@gmail.com" -GitName "Nom Cognom"
# =============================================================

param(
    [Parameter(Mandatory=$true)]
    [string]$GitHubUser,

    [Parameter(Mandatory=$false)]
    [string]$RepoName = "AssistenciaPlus",

    [Parameter(Mandatory=$false)]
    [string]$GitEmail = "",

    [Parameter(Mandatory=$false)]
    [string]$GitName = ""
)

$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "  AssistenciaPlus - Inicialitzacio Git   " -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host ""

# ── Verificar que estem al directori correcte ────────────────
if (-not (Test-Path "docker-compose.yml")) {
    Write-Host "ERROR: Executa aquest script des de l'arrel del projecte (on hi ha docker-compose.yml)" -ForegroundColor Red
    exit 1
}

# ── Verificar git instal·lat ─────────────────────────────────
try {
    $gitVersion = git --version 2>&1
    Write-Host "Git detectat: $gitVersion" -ForegroundColor Gray
} catch {
    Write-Host "ERROR: Git no esta instal·lat." -ForegroundColor Red
    Write-Host "Descarrega'l de: https://git-scm.com/download/win" -ForegroundColor Yellow
    exit 1
}

# ── Configurar identitat git (opcional) ──────────────────────
if ($GitEmail -ne "") {
    git config user.email $GitEmail
    Write-Host "Git email configurat: $GitEmail" -ForegroundColor Gray
}
if ($GitName -ne "") {
    git config user.name $GitName
    Write-Host "Git name configurat: $GitName" -ForegroundColor Gray
}

# ── Assegurar que .env no s'inclou ───────────────────────────
if (Test-Path ".gitignore") {
    $content = Get-Content ".gitignore" -Raw
    if ($content -notmatch "^\.env$") {
        Add-Content ".gitignore" "`n.env"
        Write-Host ".env afegit al .gitignore" -ForegroundColor Gray
    }
}

# ── Inicialitzar repositori ──────────────────────────────────
Write-Host ""
Write-Host "Inicialitzant repositori Git local..." -ForegroundColor Yellow
git init
git branch -M main

# ── Primer commit ────────────────────────────────────────────
Write-Host "Afegint fitxers i creant primer commit..." -ForegroundColor Yellow
git add .
git status --short

git commit -m "feat: estructura inicial del projecte AssistenciaPlus v0.1.0

- Model de domini complet (Core, Infrastructure, Api, Shared, Web)
- Docker Compose: nginx, web, api, db (PostgreSQL), redis, ollama
- Multi-idioma: catala i castella
- CI/CD GitHub Actions
- Scripts de desplegament Linux
- README.md, INSTALL.md, CHANGELOG.md"

Write-Host ""
Write-Host "Commit inicial creat!" -ForegroundColor Green

# ── Connectar i pujar a GitHub ───────────────────────────────
$repoUrl = "https://github.com/$GitHubUser/$RepoName.git"

Write-Host ""
Write-Host "Connectant amb GitHub..." -ForegroundColor Yellow
Write-Host "URL: $repoUrl" -ForegroundColor Gray
Write-Host ""
Write-Host "IMPORTANT: Assegura't d'haver creat el repositori buit a GitHub abans:" -ForegroundColor Yellow
Write-Host "  https://github.com/new" -ForegroundColor Cyan
Write-Host ""

$resposta = Read-Host "El repositori ja esta creat a GitHub? (s/n)"
if ($resposta -ne "s" -and $resposta -ne "S") {
    Write-Host ""
    Write-Host "Obre https://github.com/new, crea el repo '$RepoName' (buit, sense README)" -ForegroundColor Yellow
    Write-Host "Despres torna a executar des de la linia 'git remote add':" -ForegroundColor Yellow
    Write-Host "  git remote add origin $repoUrl" -ForegroundColor Cyan
    Write-Host "  git push -u origin main" -ForegroundColor Cyan
    exit 0
}

git remote add origin $repoUrl

Write-Host "Pujant codi (pot demanar credencials GitHub)..." -ForegroundColor Yellow
git push -u origin main

Write-Host ""
Write-Host "=========================================" -ForegroundColor Green
Write-Host "  REPOSITORI PUBLICAT A GITHUB!          " -ForegroundColor Green
Write-Host "=========================================" -ForegroundColor Green
Write-Host ""
Write-Host "URL: $repoUrl" -ForegroundColor Cyan
Write-Host ""
Write-Host "Proxims passos:" -ForegroundColor Yellow
Write-Host ""
Write-Host "  1. Configura els Secrets a GitHub per al CI/CD:" -ForegroundColor White
Write-Host "     https://github.com/$GitHubUser/$RepoName/settings/secrets/actions" -ForegroundColor Cyan
Write-Host "     Afegeix:"
Write-Host "       SERVER_HOST     -> IP o domini del servidor Linux"
Write-Host "       SERVER_USER     -> usuari SSH (p.ex. ubuntu)"
Write-Host "       SERVER_SSH_KEY  -> contingut de la clau privada SSH (~/.ssh/id_rsa)"
Write-Host ""
Write-Host "  2. Al servidor Linux, executa el desplegament inicial:" -ForegroundColor White
Write-Host "     bash scripts/deploy.sh --full-deploy" -ForegroundColor Cyan
Write-Host ""
