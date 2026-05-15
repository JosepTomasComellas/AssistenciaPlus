# AssistenciaPlus — Model de Dades
*Sprint 1 · Versió 1.0*

## Diagrama de relacions

```
AnyAcademic ──< DiaCalendari
     │
     └──< Grup >── Curs >── Cicle
              │
              ├──< Alumne ──< AssistenciaAlumne >── RegistreAssistencia
              │                                              │
              └──< RegistreAssistencia >── FranjaHoraria     │
                          │                                  │
                        Usuari (Mestre)                      │
                                                    RegistreAssistencia
```

## Taules

### `anys_academics`
| Camp | Tipus | Notes |
|---|---|---|
| id | UUID PK | |
| nom | VARCHAR(20) | "2025-2026" |
| data_inici / data_fi | DATE | |
| es_actiu | BOOL | Només 1 actiu |
| inici_t1..fi_t3 | DATE | Dates trimestres |

### `dies_calendari`
| Camp | Tipus | Notes |
|---|---|---|
| id | UUID PK | |
| any_academic_id | UUID FK | |
| data | DATE | UNIQUE per any |
| tipus_dia | INT | 0=Lectiu, 1=Festiu, 2=Intensiu, 3=NoLectiu |
| descripcio | VARCHAR(120) | Opcional |

### `cicles`
| Camp | Tipus | Notes |
|---|---|---|
| id | UUID PK | |
| nom | VARCHAR(60) | "Cicle Infantil"... |
| ordre | INT | Ordre en informes |

**Valors inicials:** Cicle Infantil (1), Cicle Inicial (2), Cicle Mitjà (3), Cicle Superior (4)

### `cursos`
| Camp | Tipus | Notes |
|---|---|---|
| id | UUID PK | |
| cicle_id | UUID FK | |
| nom | VARCHAR(60) | "Infantil 3", "1r"... |
| codi | VARCHAR(10) | "I3", "1r", "2n"... |
| ordre | INT | |
| usa_mode_fusteta | BOOL | True: I3,I4,I5,1r |

### `grups`
| Camp | Tipus | Notes |
|---|---|---|
| id | UUID PK | |
| curs_id | UUID FK | |
| any_academic_id | UUID FK | |
| lletra | VARCHAR(2) | "A","B" o buit (infantil) |
| tutor_id | UUID FK NULL | → usuaris |
> UNIQUE (curs_id, any_academic_id, lletra)

### `alumnes`
| Camp | Tipus | Notes |
|---|---|---|
| id | UUID PK | |
| nom / cognom1 / cognom2 | VARCHAR(60) | |
| data_naixement | DATE NULL | |
| foto_path | TEXT NULL | Ruta relativa |
| email_familia | VARCHAR(120) NULL | Per notificacions |
| grup_id | UUID FK | |
| ordre_fusteta | INT | Ordre al mode infantil |
| es_actiu | BOOL | Soft delete funcional |

### `franjes_horaries`
| Camp | Tipus | Notes |
|---|---|---|
| id | UUID PK | |
| nom | VARCHAR(40) | "Matí 1"..."Tarda 2" |
| hora_inici / hora_fi | TIME | |
| es_mati | BOOL | |
| es_jornada_intensiva | BOOL | True = inclosa jornada intensiva |
| ordre | INT | |

**Valors fixos:**
| # | Nom | Inici | Fi | Matí | Intensiva |
|---|---|---|---|---|---|
| 1 | Matí 1 | 09:00 | 10:00 | ✓ | ✓ |
| 2 | Matí 2 | 10:00 | 11:00 | ✓ | ✓ |
| 3 | Matí 3 | 11:00 | 11:30 | ✓ | ✓ |
| 4 | Matí 4 | 11:30 | 12:30 | ✓ | ✓ |
| 5 | Tarda 1 | 15:00 | 15:45 | ✗ | ✗ |
| 6 | Tarda 2 | 15:45 | 16:30 | ✗ | ✗ |

### `registres_assistencia`
| Camp | Tipus | Notes |
|---|---|---|
| id | UUID PK | |
| grup_id | UUID FK | |
| franja_horaria_id | UUID FK | |
| data | DATE | |
| mestre_id | UUID FK | Qui ha passat llista |
| degat_at | TIMESTAMP | Quan s'ha desat |
| observacio | VARCHAR(500) NULL | Nota general de sessió |
> UNIQUE (grup_id, franja_horaria_id, data)

### `assistencies_alumnes`
| Camp | Tipus | Notes |
|---|---|---|
| id | UUID PK | |
| registre_assistencia_id | UUID FK | CASCADE delete |
| alumne_id | UUID FK | |
| estat | INT | 0=Present, 1=Absent, 2=Tard, 3=AbsenciaParcial |
| motiu | INT NULL | 0=SenseMotiu, 1=Metge, 2=NoEsTrobaBe, 3=ServeiExtern, 4=FamiliaVeBuscar, 5=Tard |
| torna | BOOL NULL | Per absències parcials |
| minuts_retard | INT NULL | Per retards |
| observacio | VARCHAR(500) NULL | Nota individual |
| aplicat_resta_dia | BOOL | True si s'ha marcat la resta del dia |
> UNIQUE (registre_assistencia_id, alumne_id)

### `usuaris`
| Camp | Tipus | Notes |
|---|---|---|
| id | UUID PK | |
| nom / cognom1 / cognom2 | VARCHAR(60) | |
| email | VARCHAR(120) UNIQUE | |
| password_hash | VARCHAR(256) | BCrypt |
| rol | INT | 0=Mestre, 1=EquipDirectiu, 2=Administratiu |
| foto_path | TEXT NULL | |
| es_actiu | BOOL | |
| idioma | VARCHAR(2) | "ca" o "es" |
| ultim_acces | TIMESTAMP NULL | |

## Càlcul de percentatges d'absència

```
Dies lectius reals = COUNT(dies_calendari WHERE tipus_dia IN (Lectiu, JornadaIntensiva))

Franges per dia lectiu normal    = 6
Franges per dia jornada intensiva = 4

Total franges possibles = SUM per cada dia lectiu de les franges aplicables

% absència = (franges absents + franges tard) / total franges possibles × 100
```

## Llindars d'alerta

| Llindar | Informe mensual | Informe trimestral |
|---|---|---|
| >10% | ⚠️ Alerta | ⚠️ Alerta |
| >20% | — | 🔴 Crític |
| >25% | 🔴 Crític | — |
