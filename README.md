# AssistenciaPlus

> Aplicació web de gestió d'assistència per a una escola de primària catalana.

[![CI/CD](https://github.com/JosepTomasComellas/AssistenciaPlus/actions/workflows/ci-cd.yml/badge.svg)](https://github.com/JosepTomasComellas/AssistenciaPlus/actions)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com)
[![Blazor](https://img.shields.io/badge/Blazor-WASM-512BD4)](https://blazor.net)
[![MudBlazor](https://img.shields.io/badge/UI-MudBlazor-594AE2)](https://mudblazor.com)
[![Version](https://img.shields.io/badge/versió-0.4.0-blue)](CHANGELOG.md)

---

## Descripció

AssistenciaPlus digitalitza el procés de passar llista en una escola de primària. Inclou:

- **Mode normal** (graella de targetes) per a 2n–6è
- **Mode Fusteta Digital** (PDI) per a Infantil i 1r: mode desfilada (un alumne per pantalla) i mode graella gran
- **Absències parcials**: motiu, si torna o no, propagació automàtica a la resta del dia
- **Retards**: modal automàtic amb motiu i minuts
- **Informes** mensuals i trimestrals per grup i per cicle (PDF + Excel + correu automàtic)
- **Calendari escolar**: gestió de festius i jornades via UI, importació ICS i PDF (IA local), càlcul exacte de percentatges

---

## Arquitectura

```
Internet (4446) ──► Router NAT ──► Nginx SSL (443) ──► Web Blazor WASM
Xarxa interna (443) ─────────────────────────────────► API ASP.NET Core 10
                                                              │
                                                     PostgreSQL 16 + Redis 7
                                                              │
                                                        Ollama (IA local)
```

### Xarxes Docker

| Xarxa | Tipus | Serveis |
|-------|-------|---------|
| `frontend` | bridge | nginx, web, api |
| `backend` | bridge internal | api, db, redis |
| `ai` | bridge (internet) | api, ollama |

### Projectes .NET

| Projecte | Rol |
|----------|-----|
| `AssistenciaPlus.Domain` | Entitats del domini en català (model canònic) |
| `AssistenciaPlus.Application` | Interfícies, DTOs, serveis de negoci |
| `AssistenciaPlus.Infrastructure` | EF Core, repositoris, Redis, Email, Excel, Ollama |
| `AssistenciaPlus.API` | Controllers REST + SignalR Hub |
| `AssistenciaPlus.Shared` | DTOs compartits amb el frontend |
| `AssistenciaPlus.Web` | Frontend Blazor WASM + MudBlazor |

---

## Rols d'usuari

| Rol | Capacitats |
|-----|-----------|
| **Mestre** | Passa llista als seus grups. Pot registrar retards i absències parcials. |
| **Equip Directiu** | Tot el que pot el mestre + modificar registres d'altres + gestió completa. |
| **Administratiu** | Consulta d'assistències i informes. Sense edició. |

---

## Estructura escolar suportada

| Cicle | Cursos | Grups | Mode per defecte |
|-------|--------|-------|-----------------|
| Infantil | I3, I4, I5 | 1 per curs | Fusteta Digital |
| Inicial | 1r, 2n | A, B | Fusteta Digital (1r) / Normal (2n) |
| Mitjà | 3r, 4t | A, B | Normal |
| Superior | 5è, 6è | A, B | Normal |

---

## Inici ràpid

```bash
# 1. Clonar
git clone https://github.com/JosepTomasComellas/AssistenciaPlus.git
cd AssistenciaPlus

# 2. Configurar l'entorn
cp .env.example .env
# Editar .env  →  APP_PORT=443, JWT_SECRET, DB_PASSWORD, SMTP...

# 3. Afegir els certificats SSL (no estan al repositori)
mkdir -p nginx/certs
cp /ruta/fullchain.pem nginx/certs/
cp /ruta/privkey.pem  nginx/certs/

# 4. Aixecar
docker compose up -d

# 5. Accedir (xarxa local)
open https://IP_SERVIDOR
# Accedir (internet)
open https://tuteapps.ddns.net:4446
```

Credencials inicials (canviar al primer accés):
- **Email:** `admin@escola.cat`
- **Contrasenya:** `Admin1234!`

> **Ollama (IA):** El model `llama3.2` es descarrega automàticament al primer inici del contenidor. Requereix accés a internet des del servidor (~2 GB).

---

## Estructura de fitxers

```
AssistenciaPlus/
├── VERSION                     # Versió actual del projecte
├── CHANGELOG.md
├── .env                        # Configuració del servidor (NO al repo)
├── .env.example                # Plantilla de configuració
├── docker-compose.yml
├── nginx/
│   ├── nginx.conf
│   └── certs/                  # Certificats SSL (NO al repo)
├── src/
│   ├── AssistenciaPlus.sln
│   ├── AssistenciaPlus.Domain/         # Entitats català (model canònic)
│   ├── AssistenciaPlus.Application/    # Interfaces, DTOs, serveis
│   │   ├── Interfaces/
│   │   ├── DTOs/
│   │   └── Services/
│   ├── AssistenciaPlus.Infrastructure/ # EF Core, repositoris, Email, Ollama...
│   ├── AssistenciaPlus.Api/            # Controllers REST + SignalR
│   ├── AssistenciaPlus.Shared/         # DTOs compartits
│   └── AssistenciaPlus.Web/            # Blazor WASM + MudBlazor
└── scripts/
    ├── Setup-Git.ps1           # Inicialitza git i puja a GitHub (Windows)
    ├── Publish-Changes.ps1     # Publica canvis a GitHub (Windows)
    └── deploy.sh               # Desplegament al servidor Linux
```

---

## Endpoints de l'API (v0.4.0)

| Mètode | Ruta | Descripció |
|--------|------|-----------|
| `POST` | `/api/auth/login` | Autenticació (públic) |
| `POST` | `/api/auth/canvi-contrasenya` | Canvi de contrasenya |
| `GET` | `/api/auth/jo` | Dades de l'usuari autenticat |
| `GET` | `/api/grups` | Grups del mestre autenticat |
| `GET` | `/api/grups/franjes` | Franges horàries del dia |
| `GET` | `/api/alumnes?grupId=` | Alumnes d'un grup |
| `POST` | `/api/assistencia` | Desar sessió completa |
| `POST` | `/api/assistencia/absencia-parcial` | Aplicar a resta del dia |
| `GET` | `/api/assistencia/percentatge/{alumneId}` | % absència en rang |
| `GET` | `/api/informes/mensual/grup/{id}` | Informe mensual per grup |
| `GET` | `/api/informes/trimestral/grup/{id}` | Informe trimestral per grup |
| `POST` | `/api/informes/enviar-mensual` | Enviar informe per correu |
| `GET` | `/api/configuracio/calendari/{anyId}` | Dies especials del calendari |
| `PUT` | `/api/configuracio/calendari/dia` | Afegir o actualitzar un dia |
| `DELETE` | `/api/configuracio/calendari/dia/{anyId}/{data}` | Esborrar un dia |
| `POST` | `/api/configuracio/calendari/{anyId}/importar-ics` | Importar fitxer ICS |
| `POST` | `/api/configuracio/calendari/{anyId}/importar-pdf` | Importar PDF via IA (Ollama) |
| `GET` | `/api/configuracio/anys-academics` | Llistar anys acadèmics |
| `POST` | `/api/configuracio/anys-academics` | Crear any acadèmic |
| `POST` | `/api/configuracio/anys-academics/{id}/activar` | Activar any acadèmic |
| `GET` | `/api/configuracio/usuaris` | Gestió d'usuaris (EquipDirectiu) |

Swagger disponible a `/swagger` en entorn de desenvolupament.

---

## Tecnologies

- **Frontend**: Blazor WebAssembly, MudBlazor 9.4, SignalR Client
- **Backend**: ASP.NET Core 10, Entity Framework Core 10
- **Base de dades**: PostgreSQL 16
- **Cache / Sessions**: Redis 7
- **IA local**: Ollama (llama3.2) — extracció de calendaris PDF en llenguatge natural
- **Extracció PDF**: PdfPig 0.1.9
- **Proxy**: Nginx 1.25 (SSL/TLS)
- **Contenidors**: Docker Compose
- **CI/CD**: GitHub Actions
- **Logging**: Serilog (consola + fitxer rotatiu, health checks filtrats)
- **Autenticació**: JWT Bearer + BCrypt

---

## Versionat i publicació

El projecte segueix [Semantic Versioning](https://semver.org/). Cada canvi es documenta al [CHANGELOG.md](CHANGELOG.md).

Per publicar canvis des de Windows:
```powershell
.\scripts\Publish-Changes.ps1 -Message "feat: nova funcionalitat"
```

---

## Llicència i contacte

Ús intern de l'escola. Per incidències: [Issues de GitHub](https://github.com/JosepTomasComellas/AssistenciaPlus/issues).
