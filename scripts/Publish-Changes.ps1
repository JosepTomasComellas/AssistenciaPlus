# =============================================================
# Publish-Changes.ps1  (Windows PowerShell)
# Fa commit dels canvis locals i els puja a GitHub.
# El CI/CD de GitHub Actions s'encarrega del desplegament.
#
# Ús:
#   .\scripts\Publish-Changes.ps1 -Message "fix: corregit error en l'assistència"
#   .\scripts\Publish-Changes.ps1 -Message "feat: afegida pàgina d'informes" -Branch "develop"
# =============================================================

param(
    [Parameter(Mandatory=$true)]
    [string]$Message,

    [Parameter(Mandatory=$false)]
    [string]$Branch = "main",

    # Si s'especifica, crea una branca nova abans de fer el commit
    [Parameter(Mandatory=$false)]
    [string]$NewBranch = "",

    # Pujar sense fer preguntes
    [switch]$Force
)

$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "  AssistenciaPlus - Publicar canvis      " -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host ""

# ── Verificar que estem al directori correcte ────────────────
if (-not (Test-Path "docker-compose.yml")) {
    Write-Host "ERROR: Executa des de l'arrel del projecte" -ForegroundColor Red
    exit 1
}

# ── Mostrar estat actual ──────────────────────────────────────
Write-Host "Estat del repositori:" -ForegroundColor Yellow
git status --short
Write-Host ""

# ── Verificar que hi ha canvis ────────────────────────────────
$canvis = git status --porcelain
if (-not $canvis) {
    Write-Host "No hi ha canvis per publicar." -ForegroundColor Yellow
    exit 0
}

# ── Crear branca nova si s'ha demanat ────────────────────────
if ($NewBranch -ne "") {
    Write-Host "Creant branca: $NewBranch" -ForegroundColor Yellow
    git checkout -b $NewBranch
    $Branch = $NewBranch
}

# ── Confirmació (si no és -Force) ────────────────────────────
if (-not $Force) {
    Write-Host "Missatge del commit: $Message" -ForegroundColor White
    Write-Host "Branca destí:        $Branch" -ForegroundColor White
    Write-Host ""
    $resposta = Read-Host "Confirmes la publicació? (s/n)"
    if ($resposta -ne "s" -and $resposta -ne "S") {
        Write-Host "Cancel·lat." -ForegroundColor Yellow
        exit 0
    }
}

# ── Commit i push ─────────────────────────────────────────────
Write-Host ""
Write-Host "Afegint canvis..." -ForegroundColor Yellow
git add .

Write-Host "Creant commit..." -ForegroundColor Yellow
git commit -m $Message

Write-Host "Pujant a GitHub (branca: $Branch)..." -ForegroundColor Yellow
git push origin $Branch

Write-Host ""
Write-Host "=========================================" -ForegroundColor Green
Write-Host "  CANVIS PUBLICATS!                      " -ForegroundColor Green
Write-Host "=========================================" -ForegroundColor Green
Write-Host ""

# ── Informar sobre el CI/CD ───────────────────────────────────
if ($Branch -eq "main") {
    Write-Host "La pipeline CI/CD s'ha iniciat automaticament." -ForegroundColor Cyan
    Write-Host "Segueix el progres a GitHub Actions:" -ForegroundColor White

    # Intentar obtenir la URL del remote
    $remoteUrl = git remote get-url origin 2>$null
    if ($remoteUrl -match "github\.com[:/](.+)\.git") {
        $repoPath = $Matches[1]
        Write-Host "  https://github.com/$repoPath/actions" -ForegroundColor Cyan
    }
    Write-Host ""
    Write-Host "Fases del desplegament:" -ForegroundColor White
    Write-Host "  1. Build & Test   (~2 min)"
    Write-Host "  2. Docker Build   (~5 min)"
    Write-Host "  3. Deploy SSH     (~1 min)"
    Write-Host ""
    Write-Host "El servidor s'actualitzara automaticament en ~8 min." -ForegroundColor Green
} else {
    Write-Host "Branca '$Branch' publicada. No s'ha iniciat desplegament automatic." -ForegroundColor Yellow
    Write-Host "Per desplegar, fusiona amb main via Pull Request." -ForegroundColor White
}
Write-Host ""
