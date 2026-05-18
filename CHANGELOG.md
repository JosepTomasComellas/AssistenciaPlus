# Changelog - AssistenciaPlus

Tots els canvis importants d'aquest projecte es documenten en aquest fitxer.

Format basat en [Keep a Changelog](https://keepachangelog.com/ca/1.0.0/).
Segueix [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [Unreleased]

### Pendent d'implementar
- Importació d'alumnes des de fitxer Excel (format Esfera/Alexia)
- Generació d'informes en PDF
- Migració d'any acadèmic (còpia de grups per al curs nou)
- **Ollama runner crash** (exit code -1): el model `llama3.2` s'ha descarregat però el procés d'inferència peta en carregar els tensors (probablement `vm.overcommit_memory` al LXC o fitxer corrupte — pendent de diagnosi)

---

## [0.4.0] - 2026-05-18

### Afegit

**Calendari escolar (`/configuracio/calendari`)**
- Pàgina de gestió completa del calendari escolar per a l'Equip Directiu
- Selector d'any acadèmic (permet gestionar calendaris d'anys anteriors i futurs)
- Taula de dies especials (festius, no lectius, jornada intensiva) amb accions d'editar i esborrar
- Diàleg d'afegir/editar dia amb datepicker (limitat al rang de l'any seleccionat), tipus i descripció
- Confirmació de l'esborrat via `MudMessageBox`

**Importació de calendari**
- **ICS** (`POST /api/configuracio/calendari/{id}/importar-ics`): importació de fitxers `.ics` estàndard (VEVENT amb DTSTART/DTEND/SUMMARY), suporta line-folding i rangs de dates
- **PDF via IA** (`POST /api/configuracio/calendari/{id}/importar-pdf`): extracció de text amb **PdfPig** + parsing del calendari amb **Ollama (llama3.2)** — retorna dies especials en JSON

**Eliminació de dia de calendari**
- `DELETE /api/configuracio/calendari/dia/{anyAcademicId}/{data}` — nou endpoint i `EsborrarDiaAsync` al repositori

**Ollama (IA local)**
- Nova mètode `ParsearCalendariPdfAsync` a `OllamaService`
- Detecció de model no descarregat (HTTP 404 → `InvalidOperationException` amb missatge accionable)
- `docker-compose.yml`: Ollama movida de la xarxa `backend` (internal) a nova xarxa `ai` (bridge amb internet) — resol el bloqueig DNS per descarregar models
- DNS explícit (8.8.8.8 / 8.8.4.4) al contenidor Ollama
- Entrypoint automàtic: `ollama serve & sleep 10 && ollama pull <model>` — descarrega el model automàticament al primer inici

### Corregit
- **Avatar capçalera**: imatge de l'usuari es mostrava fora de lloc i a tamany natural; corregit amb contenidor explícit `width:40px;height:40px` i overlay `position:absolute`
- **Inicials de l'avatar**: si `_usuariActual` és null, fa fallback al nom del JWT (`context.User.Identity?.Name`)
- **Logs de health check**: `/health` ara es registra a nivell `Verbose` (filtrat de la consola) — elimina el missatge de log cada 30 s
- **Favicon**: substituït pel logo real de l'Escola Marta Mata

### Canviat
- `NavMenu.razor`: afegit enllaç "Calendari escolar" (`/configuracio/calendari`) al menú de configuració
- `ApiModels.cs`: afegits `DiaCalendariModel` i `ActualitzarDiaCalendariModel`
- `ConfiguracioService.cs`: afegits `GetCalendariAsync`, `ActualitzarDiaCalendariAsync`, `EsborrarDiaCalendariAsync`, `ImportarIcsAsync`, `ImportarPdfAsync`
- `AssistenciaPlus.Api.csproj`: afegit paquet `PdfPig 0.1.9`

---

## [0.3.0] - 2026-05-15

### Canviat (Migració tecnològica)
- **Totes les capes**: migrat de .NET 8 a **.NET 10** (LTS) amb **C# 14** per defecte
- **MudBlazor**: actualitzat de 6.19.0 a **9.4.0** (afegit `MudPopoverProvider`, `MudChip<T>`)
- **EF Core + Npgsql**: 8.0.0 → 10.0.0
- **JWT Bearer**: 8.0.0 → 10.0.0
- **Serilog**: actualitzat a versions compatibles amb .NET 10
- **Swashbuckle**: 6.5.0 → 8.1.0
- **Docker**: imatges base `sdk:10.0` i `aspnet:10.0`
- **ClosedXML**: 0.102.1 → 0.104.1

---

## [0.2.0] - 2026-05-15

### Afegit (Sprint 2 — Capa d'API REST)

**Nou projecte `AssistenciaPlus.Application`** (Clean Architecture):
- Interfícies de repositoris en català: `IAlumneRepository`, `IGrupRepository`,
  `IAssistenciaRepository`, `IUsuariRepository`, `ICalendariRepository`
- DTOs en català: `AlumneDto`, `GrupDto`, `RegistreAssistenciaDto`, `AssistenciaAlumneDto`,
  `InformeMensualDto`, `InformeTrimestralDto`, `AnyAcademicDto`, etc.
- `AssistenciaService`: desar sessió completa, absència parcial resta de dia,
  càlcul de percentatge d'absència per franges reals del calendari
- `InformesService`: informes mensuals i trimestrals per grup i per cicle,
  llindars d'alerta (≥10%) i crític (≥25% mensual / ≥20% trimestral)
- `ApiResponse<T>`: format estàndard `{ success, data, error }` per a tots els endpoints

**Nou projecte `AssistenciaPlus.Domain`** (projecte .csproj creat, entitats ja existien):
- Integrat a la solució `.sln`

**Implementació de repositoris a `AssistenciaPlus.Infrastructure`**:
- `AlumneRepository`, `GrupRepository`, `AssistenciaRepository`,
  `UsuariRepository`, `CalendariRepository`
- Tots usen `Data/AppDbContext` (entitats catalanes, coincideix amb la migració real de BD)

**6 Controllers a `AssistenciaPlus.API`**:
- `AuthController` — `POST /api/auth/login`, `POST /api/auth/canvi-contrasenya`, `GET /api/auth/jo`
- `GrupsController` — grups del mestre/EquipDirectiu, franges horàries, cicles
- `AlumnesController` — llistar, crear, actualitzar, eliminar alumnes per grup
- `AssistenciaController` — desar sessió completa, absència parcial, % absència, SignalR
- `InformesController` — mensual/trimestral per grup i cicle, enviament per correu SMTP
- `ConfiguracioController` — gestió calendari escolar + CRUD usuaris (EquipDirectiu)

**Configuració**:
- `appsettings.json` i `appsettings.Development.json` amb totes les claus documentades
- `Escola:EmailInformes` configurable (per defecte `a8059721@xtec.cat`)
- Polítiques JWT: `NomesEquipDirectiu`, `NomesEquipDirectiuIAdministratiu`, `QualsevolRol`
- `docker-compose.yml`: afegida variable `ESCOLA_EMAIL_INFORMES`
- `VERSION` i `CHANGELOG.md` establerts

### Canviat
- `Program.cs`: actualitzat per registrar els nous repositoris i serveis del Domain
- `.env.example`: aclariment del port (`APP_PORT=443` al servidor; 4446 és el port del router)

---

## [0.1.0] - 2026-05-15

### Afegit (Sprint 1 — Model de domini i infraestructura)

**Model de domini en català** (`AssistenciaPlus.Domain`):
- Entitats: `AnyAcademic`, `DiaCalendari` (enum `TipusDia`), `Cicle`, `Curs`,
  `Grup`, `Alumne`, `FranjaHoraria`, `RegistreAssistencia`, `AssistenciaAlumne`,
  `Usuari` (enum `RolUsuari`: Mestre / EquipDirectiu / Administratiu)
- Enums d'assistència: `EstatAssistencia` (Present/Absent/Tard/AbsenciaParcial),
  `MotiuAbsencia` (SenseMotiu/Metge/NoEsTrobaBe/ServeiExtern/FamiliaVeBuscar/Tard)
- `BaseEntity` amb `Id`, `CreatedAt`, `UpdatedAt`, `IsDeleted`

**Infraestructura** (`AssistenciaPlus.Infrastructure`):
- `AppDbContext` (Data/) amb totes les relacions, índexs i soft-delete global
- `SeedData`: cicles, cursos, 6 franges horàries fixes i usuari admin inicial
- Migració PostgreSQL inicial: esquema en català (taules: `anys_academics`, `grups`,
  `alumnes`, `registres_assistencia`, `assistencies_alumnes`, etc.)

**Estructura Docker**:
- Docker Compose: nginx, web (Blazor), api, db (PostgreSQL 16), redis, ollama
- Nginx: proxy invers SSL, ports 443 (servidor) / 4446 (extern via router)
- CI/CD GitHub Actions: build .NET → deploy SSH (`deploy.sh --update`)
- Scripts: `Setup-Git.ps1`, `Publish-Changes.ps1`, `deploy.sh`

**Altres**:
- Autenticació JWT, Redis per a sessions, Serilog estructurat
- Integració Ollama (IA local), SignalR Hub, ClosedXML (Excel), BCrypt
- Multi-idioma: català (defecte) i castellà — fitxers `i18n/ca.json`, `i18n/es.json`
- `AssistenciaPlus.Core` (model alternatiu anglès, conservat per compatibilitat)
- `AssistenciaPlus.Shared` amb DTOs en anglès (per al frontend Blazor)
