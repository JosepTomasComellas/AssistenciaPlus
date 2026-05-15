# Changelog - AssistenciaPlus

Tots els canvis importants d'aquest projecte es documenten en aquest fitxer.

Format basat en [Keep a Changelog](https://keepachangelog.com/ca/1.0.0/).
Segueix [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [Unreleased]

### Pendent d'implementar
- Pàgines Blazor: Dashboard, Passar llista, Mode Fusteta Digital, Informes, Configuració
- Importació d'alumnes des de fitxer Excel (format Esfera/Alexia)
- Generació d'informes en PDF
- Migració d'any acadèmic (còpia de grups per al curs nou)
- Gestió de l'any acadèmic i grups via UI

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
