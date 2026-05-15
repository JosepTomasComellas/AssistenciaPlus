using AssistenciaPlus.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AssistenciaPlus.Infrastructure.Data.Seed;

/// <summary>
/// Dades inicials del sistema: cicles, cursos i franges horàries.
/// S'executa una sola vegada en el primer desplegament.
/// </summary>
public static class SeedData
{
    public static async Task InitializeAsync(AppDbContext context)
    {
        await SeedCiclesICursosAsync(context);
        await SeedFranjesHorariesAsync(context);
        await SeedAdminUsuariAsync(context);
        await SeedAnyAcademicAsync(context);

        await context.SaveChangesAsync();
    }

    // ── Cicles i Cursos ────────────────────────────────────────────────
    private static async Task SeedCiclesICursosAsync(AppDbContext context)
    {
        if (await context.Cicles.AnyAsync()) return;

        var cicles = new List<Cicle>
        {
            new()
            {
                Id = Guid.Parse("10000000-0000-0000-0000-000000000001"),
                Nom = "Cicle Infantil",
                Ordre = 1,
                Cursos = new List<Curs>
                {
                    new() { Nom = "Infantil 3", Codi = "I3", Ordre = 1, UsaModeFusteta = true },
                    new() { Nom = "Infantil 4", Codi = "I4", Ordre = 2, UsaModeFusteta = true },
                    new() { Nom = "Infantil 5", Codi = "I5", Ordre = 3, UsaModeFusteta = true },
                }
            },
            new()
            {
                Id = Guid.Parse("10000000-0000-0000-0000-000000000002"),
                Nom = "Cicle Inicial",
                Ordre = 2,
                Cursos = new List<Curs>
                {
                    new() { Nom = "1r",  Codi = "1r",  Ordre = 1, UsaModeFusteta = true  },
                    new() { Nom = "2n",  Codi = "2n",  Ordre = 2, UsaModeFusteta = false },
                }
            },
            new()
            {
                Id = Guid.Parse("10000000-0000-0000-0000-000000000003"),
                Nom = "Cicle Mitjà",
                Ordre = 3,
                Cursos = new List<Curs>
                {
                    new() { Nom = "3r",  Codi = "3r",  Ordre = 1, UsaModeFusteta = false },
                    new() { Nom = "4t",  Codi = "4t",  Ordre = 2, UsaModeFusteta = false },
                }
            },
            new()
            {
                Id = Guid.Parse("10000000-0000-0000-0000-000000000004"),
                Nom = "Cicle Superior",
                Ordre = 4,
                Cursos = new List<Curs>
                {
                    new() { Nom = "5è",  Codi = "5e",  Ordre = 1, UsaModeFusteta = false },
                    new() { Nom = "6è",  Codi = "6e",  Ordre = 2, UsaModeFusteta = false },
                }
            },
        };

        await context.Cicles.AddRangeAsync(cicles);
    }

    // ── Franges Horàries ───────────────────────────────────────────────
    private static async Task SeedFranjesHorariesAsync(AppDbContext context)
    {
        if (await context.FranjesHoraries.AnyAsync()) return;

        var franjes = new List<FranjaHoraria>
        {
            // ── MATÍ ──────────────────────────────────────
            new()
            {
                Id = Guid.Parse("20000000-0000-0000-0000-000000000001"),
                Nom = "Matí 1",
                HoraInici = new TimeOnly(9, 0),
                HoraFi    = new TimeOnly(10, 0),
                EsMati = true,
                EsJornadaIntensiva = true,
                Ordre = 1
            },
            new()
            {
                Id = Guid.Parse("20000000-0000-0000-0000-000000000002"),
                Nom = "Matí 2",
                HoraInici = new TimeOnly(10, 0),
                HoraFi    = new TimeOnly(11, 0),
                EsMati = true,
                EsJornadaIntensiva = true,
                Ordre = 2
            },
            new()
            {
                Id = Guid.Parse("20000000-0000-0000-0000-000000000003"),
                Nom = "Matí 3",
                HoraInici = new TimeOnly(11, 0),
                HoraFi    = new TimeOnly(11, 30),
                EsMati = true,
                EsJornadaIntensiva = true,
                Ordre = 3
            },
            new()
            {
                Id = Guid.Parse("20000000-0000-0000-0000-000000000004"),
                Nom = "Matí 4",
                HoraInici = new TimeOnly(11, 30),
                HoraFi    = new TimeOnly(12, 30),
                EsMati = true,
                EsJornadaIntensiva = true,
                Ordre = 4
            },
            // ── TARDA ─────────────────────────────────────
            new()
            {
                Id = Guid.Parse("20000000-0000-0000-0000-000000000005"),
                Nom = "Tarda 1",
                HoraInici = new TimeOnly(15, 0),
                HoraFi    = new TimeOnly(15, 45),
                EsMati = false,
                EsJornadaIntensiva = false,
                Ordre = 5
            },
            new()
            {
                Id = Guid.Parse("20000000-0000-0000-0000-000000000006"),
                Nom = "Tarda 2",
                HoraInici = new TimeOnly(15, 45),
                HoraFi    = new TimeOnly(16, 30),
                EsMati = false,
                EsJornadaIntensiva = false,
                Ordre = 6
            },
        };

        await context.FranjesHoraries.AddRangeAsync(franjes);
    }

    // ── Any Acadèmic 2025-2026 ─────────────────────────────────────────
    private static async Task SeedAnyAcademicAsync(AppDbContext context)
    {
        if (await context.AnysAcademics.AnyAsync()) return;

        var anyAcademic = new AnyAcademic
        {
            Id = Guid.Parse("40000000-0000-0000-0000-000000000001"),
            Nom = "2025-2026",
            DataInici = new DateOnly(2025, 9, 9),
            DataFi = new DateOnly(2026, 6, 19),
            EsActiu = true,
            IniciT1 = new DateOnly(2025, 9, 9),
            FiT1 = new DateOnly(2025, 12, 19),
            IniciT2 = new DateOnly(2026, 1, 8),
            FiT2 = new DateOnly(2026, 3, 27),
            IniciT3 = new DateOnly(2026, 4, 14),
            FiT3 = new DateOnly(2026, 6, 19),
        };

        await context.AnysAcademics.AddAsync(anyAcademic);
    }

    // ── Usuari Admin inicial ───────────────────────────────────────────
    private static async Task SeedAdminUsuariAsync(AppDbContext context)
    {
        if (await context.Usuaris.AnyAsync()) return;

        // Contrasenya per defecte: "Admin1234!" — s'ha de canviar al primer accés
        var admin = new Usuari
        {
            Id = Guid.Parse("30000000-0000-0000-0000-000000000001"),
            Nom = "Administrador",
            Cognom1 = "Sistema",
            Email = "admin@escola.cat",
            PasswordHash = "$2a$12$yH271dKnnTywNUgRjsTyxO1PvC15D8MUoeSkf4JRYS2MT65GMyNwy",
            Rol = RolUsuari.EquipDirectiu,
            EsActiu = true,
            Idioma = "ca"
        };

        await context.Usuaris.AddAsync(admin);
    }
}
