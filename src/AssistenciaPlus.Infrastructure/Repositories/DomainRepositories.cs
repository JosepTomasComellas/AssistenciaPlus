using AssistenciaPlus.Application.Interfaces;
using AssistenciaPlus.Domain.Entities;
using AssistenciaPlus.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using DomainCtx = AssistenciaPlus.Infrastructure.Data.AppDbContext;

namespace AssistenciaPlus.Infrastructure.Repositories;

// ═══════════════════════════════════════════════════════════
//  REPOSITORI D'ALUMNES
// ═══════════════════════════════════════════════════════════

public class AlumneRepository : IAlumneRepository
{
    private readonly DomainCtx _ctx;
    public AlumneRepository(DomainCtx ctx) => _ctx = ctx;

    public Task<Alumne?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _ctx.Alumnes.Include(a => a.Grup).FirstOrDefaultAsync(a => a.Id == id, ct);

    public async Task<IReadOnlyList<Alumne>> GetPerGrupAsync(Guid grupId, CancellationToken ct = default)
        => await _ctx.Alumnes
            .Include(a => a.Grup).ThenInclude(g => g.Curs).ThenInclude(c => c.Cicle)
            .Where(a => a.GrupId == grupId && a.EsActiu)
            .OrderBy(a => a.OrdreFusteta).ThenBy(a => a.Cognom1).ThenBy(a => a.Nom)
            .ToListAsync(ct);

    public async Task<Alumne> AfegirAsync(Alumne alumne, CancellationToken ct = default)
    {
        await _ctx.Alumnes.AddAsync(alumne, ct);
        return alumne;
    }

    public Task ActualitzarAsync(Alumne alumne, CancellationToken ct = default)
    {
        _ctx.Entry(alumne).State = EntityState.Modified;
        return Task.CompletedTask;
    }

    public async Task EsborrarAsync(Guid id, CancellationToken ct = default)
    {
        var alumne = await _ctx.Alumnes.FindAsync(new object[] { id }, ct);
        if (alumne != null)
        {
            alumne.IsDeleted = true;
            alumne.EsActiu = false;
            _ctx.Entry(alumne).State = EntityState.Modified;
        }
    }

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => _ctx.SaveChangesAsync(ct);
}

// ═══════════════════════════════════════════════════════════
//  REPOSITORI DE GRUPS
// ═══════════════════════════════════════════════════════════

public class GrupRepository : IGrupRepository
{
    private readonly DomainCtx _ctx;
    public GrupRepository(DomainCtx ctx) => _ctx = ctx;

    public Task<Grup?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _ctx.Grups
            .Include(g => g.Curs).ThenInclude(c => c.Cicle)
            .Include(g => g.AnyAcademic)
            .Include(g => g.Tutor)
            .FirstOrDefaultAsync(g => g.Id == id, ct);

    public Task<Grup?> GetByIdAmbDetallsAsync(Guid id, CancellationToken ct = default)
        => _ctx.Grups
            .Include(g => g.Curs).ThenInclude(c => c.Cicle)
            .Include(g => g.AnyAcademic)
            .Include(g => g.Tutor)
            .Include(g => g.Alumnes.Where(a => a.EsActiu))
            .FirstOrDefaultAsync(g => g.Id == id, ct);

    public async Task<IReadOnlyList<Grup>> GetPerMestreAsync(
        Guid mestreId, Guid anyAcademicId, CancellationToken ct = default)
        => await _ctx.Grups
            .Include(g => g.Curs).ThenInclude(c => c.Cicle)
            .Include(g => g.AnyAcademic)
            .Include(g => g.Tutor)
            .Include(g => g.Alumnes.Where(a => a.EsActiu))
            .Where(g => g.AnyAcademicId == anyAcademicId
                && (g.TutorId == mestreId
                    || g.MestresAutoritzats.Any(m => m.UsuariId == mestreId)))
            .OrderBy(g => g.Curs.Cicle.Ordre).ThenBy(g => g.Curs.Ordre).ThenBy(g => g.Lletra)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Grup>> GetPerAnyAcademicAsync(
        Guid anyAcademicId, CancellationToken ct = default)
        => await _ctx.Grups
            .Include(g => g.Curs).ThenInclude(c => c.Cicle)
            .Include(g => g.AnyAcademic)
            .Include(g => g.Tutor)
            .Include(g => g.Alumnes.Where(a => a.EsActiu))
            .Where(g => g.AnyAcademicId == anyAcademicId)
            .OrderBy(g => g.Curs.Cicle.Ordre).ThenBy(g => g.Curs.Ordre).ThenBy(g => g.Lletra)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Grup>> GetPerCicleAsync(
        Guid cicleId, Guid anyAcademicId, CancellationToken ct = default)
        => await _ctx.Grups
            .Include(g => g.Curs).ThenInclude(c => c.Cicle)
            .Include(g => g.AnyAcademic)
            .Include(g => g.Tutor)
            .Include(g => g.Alumnes.Where(a => a.EsActiu))
            .Where(g => g.Curs.CicleId == cicleId && g.AnyAcademicId == anyAcademicId)
            .OrderBy(g => g.Curs.Ordre).ThenBy(g => g.Lletra)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Cicle>> GetCiclesAsync(CancellationToken ct = default)
        => await _ctx.Cicles
            .Include(c => c.Cursos)
            .OrderBy(c => c.Ordre)
            .ToListAsync(ct);

    public async Task<Grup> AfegirAsync(Grup grup, CancellationToken ct = default)
    {
        await _ctx.Grups.AddAsync(grup, ct);
        return grup;
    }

    public Task ActualitzarAsync(Grup grup, CancellationToken ct = default)
    {
        _ctx.Entry(grup).State = EntityState.Modified;
        return Task.CompletedTask;
    }

    public async Task EsborrarAsync(Guid id, CancellationToken ct = default)
    {
        var grup = await _ctx.Grups.FindAsync(new object[] { id }, ct);
        if (grup != null)
        {
            grup.IsDeleted = true;
            _ctx.Entry(grup).State = EntityState.Modified;
        }
    }

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => _ctx.SaveChangesAsync(ct);

    public async Task<IReadOnlyList<Usuari>> GetMestresGrupAsync(Guid grupId, CancellationToken ct = default)
        => await _ctx.GrupsMestres
            .Where(gm => gm.GrupId == grupId)
            .Include(gm => gm.Usuari)
            .Select(gm => gm.Usuari)
            .OrderBy(u => u.Cognom1).ThenBy(u => u.Nom)
            .ToListAsync(ct);

    public Task<bool> EsMestreAutoritzatAsync(Guid grupId, Guid mestreId, CancellationToken ct = default)
        => _ctx.GrupsMestres.AnyAsync(gm => gm.GrupId == grupId && gm.UsuariId == mestreId, ct);

    public async Task AfegirMestreGrupAsync(Guid grupId, Guid mestreId, CancellationToken ct = default)
    {
        var existent = await _ctx.GrupsMestres
            .AnyAsync(gm => gm.GrupId == grupId && gm.UsuariId == mestreId, ct);
        if (!existent)
            await _ctx.GrupsMestres.AddAsync(new GrupMestre
            {
                GrupId = grupId,
                UsuariId = mestreId,
                AfegitAt = DateTime.UtcNow
            }, ct);
    }

    public async Task TreureMestreGrupAsync(Guid grupId, Guid mestreId, CancellationToken ct = default)
    {
        var gm = await _ctx.GrupsMestres
            .FirstOrDefaultAsync(x => x.GrupId == grupId && x.UsuariId == mestreId, ct);
        if (gm != null) _ctx.GrupsMestres.Remove(gm);
    }

    public Task<Cicle?> GetCiclePerIdAsync(Guid id, CancellationToken ct = default)
        => _ctx.Cicles.Include(c => c.Cursos).FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<Cicle> AfegirCicleAsync(Cicle cicle, CancellationToken ct = default)
    {
        await _ctx.Cicles.AddAsync(cicle, ct);
        return cicle;
    }

    public Task ActualitzarCicleAsync(Cicle cicle, CancellationToken ct = default)
    {
        _ctx.Entry(cicle).State = EntityState.Modified;
        return Task.CompletedTask;
    }

    public async Task EsborrarCicleAsync(Guid id, CancellationToken ct = default)
    {
        var cicle = await _ctx.Cicles.FindAsync(new object[] { id }, ct);
        if (cicle != null)
        {
            cicle.IsDeleted = true;
            _ctx.Entry(cicle).State = EntityState.Modified;
        }
    }

    public Task<Curs?> GetCursPerIdAsync(Guid id, CancellationToken ct = default)
        => _ctx.Cursos
            .Include(c => c.Cicle)
            .Include(c => c.Grups)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<Curs> AfegirCursAsync(Curs curs, CancellationToken ct = default)
    {
        await _ctx.Cursos.AddAsync(curs, ct);
        return curs;
    }

    public Task ActualitzarCursAsync(Curs curs, CancellationToken ct = default)
    {
        _ctx.Entry(curs).State = EntityState.Modified;
        return Task.CompletedTask;
    }

    public async Task EsborrarCursAsync(Guid id, CancellationToken ct = default)
    {
        var curs = await _ctx.Cursos.FindAsync(new object[] { id }, ct);
        if (curs != null)
        {
            curs.IsDeleted = true;
            _ctx.Entry(curs).State = EntityState.Modified;
        }
    }
}

// ═══════════════════════════════════════════════════════════
//  REPOSITORI D'ASSISTÈNCIA
// ═══════════════════════════════════════════════════════════

public class AssistenciaRepository : IAssistenciaRepository
{
    private readonly DomainCtx _ctx;
    public AssistenciaRepository(DomainCtx ctx) => _ctx = ctx;

    public Task<RegistreAssistencia?> GetRegistreAsync(
        Guid grupId, Guid franjaId, DateOnly data, CancellationToken ct = default)
        => _ctx.RegistresAssistencia
            .FirstOrDefaultAsync(r => r.GrupId == grupId
                && r.FranjaHorariaId == franjaId && r.Data == data, ct);

    public Task<RegistreAssistencia?> GetRegistreAmbAssistenciesAsync(
        Guid grupId, Guid franjaId, DateOnly data, CancellationToken ct = default)
        => _ctx.RegistresAssistencia
            .Include(r => r.Grup).ThenInclude(g => g.Curs)
            .Include(r => r.FranjaHoraria)
            .Include(r => r.Mestre)
            .Include(r => r.Assistencies).ThenInclude(a => a.Alumne)
            .FirstOrDefaultAsync(r => r.GrupId == grupId
                && r.FranjaHorariaId == franjaId && r.Data == data, ct);

    public async Task<IReadOnlyList<RegistreAssistencia>> GetRegistresPerGrupIDataAsync(
        Guid grupId, DateOnly data, CancellationToken ct = default)
        => await _ctx.RegistresAssistencia
            .Include(r => r.Grup).ThenInclude(g => g.Curs)
            .Include(r => r.FranjaHoraria)
            .Include(r => r.Mestre)
            .Include(r => r.Assistencies).ThenInclude(a => a.Alumne)
            .Where(r => r.GrupId == grupId && r.Data == data)
            .OrderBy(r => r.FranjaHoraria.Ordre)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<AssistenciaAlumne>> GetAssistenciesAlumneAsync(
        Guid alumneId, DateOnly dataInici, DateOnly dataFi, CancellationToken ct = default)
        => await _ctx.AssistenciesAlumnes
            .Include(a => a.RegistreAssistencia)
            .Include(a => a.Alumne)
            .Where(a => a.AlumneId == alumneId
                && a.RegistreAssistencia.Data >= dataInici
                && a.RegistreAssistencia.Data <= dataFi)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<AssistenciaAlumne>> GetAssistenciesGrupMesAsync(
        Guid grupId, int any, int mes, CancellationToken ct = default)
    {
        var dataInici = new DateOnly(any, mes, 1);
        var dataFi = dataInici.AddMonths(1).AddDays(-1);
        return await GetAssistenciesGrupRangAsync(grupId, dataInici, dataFi, ct);
    }

    public async Task<IReadOnlyList<AssistenciaAlumne>> GetAssistenciesGrupRangAsync(
        Guid grupId, DateOnly dataInici, DateOnly dataFi, CancellationToken ct = default)
        => await _ctx.AssistenciesAlumnes
            .Include(a => a.RegistreAssistencia)
            .Include(a => a.Alumne)
            .Where(a => a.RegistreAssistencia.GrupId == grupId
                && a.RegistreAssistencia.Data >= dataInici
                && a.RegistreAssistencia.Data <= dataFi)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<RegistreAssistencia>> GetSessionsAsync(
        Guid anyAcademicId, int pagina, int midaPagina,
        Guid? grupId = null, Guid? mestreId = null,
        DateOnly? dataInici = null, DateOnly? dataFi = null,
        Guid? franjaId = null,
        string sortBy = "data", bool sortAsc = false,
        CancellationToken ct = default)
    {
        var q = _ctx.RegistresAssistencia
            .Include(r => r.Grup).ThenInclude(g => g.Curs)
            .Include(r => r.FranjaHoraria)
            .Include(r => r.Mestre)
            .Include(r => r.Assistencies)
            .Where(r => r.Grup.AnyAcademicId == anyAcademicId
                && (grupId == null || r.GrupId == grupId)
                && (mestreId == null || r.MestreId == mestreId)
                && (dataInici == null || r.Data >= dataInici)
                && (dataFi == null || r.Data <= dataFi)
                && (franjaId == null || r.FranjaHorariaId == franjaId));

        q = (sortBy.ToLower(), sortAsc) switch
        {
            ("grup", true)    => q.OrderBy(r => r.Grup.NomComplet).ThenByDescending(r => r.Data),
            ("grup", false)   => q.OrderByDescending(r => r.Grup.NomComplet).ThenByDescending(r => r.Data),
            ("mestre", true)  => q.OrderBy(r => r.Mestre.NomComplet).ThenByDescending(r => r.Data),
            ("mestre", false) => q.OrderByDescending(r => r.Mestre.NomComplet).ThenByDescending(r => r.Data),
            ("franja", true)  => q.OrderBy(r => r.FranjaHoraria.Ordre).ThenByDescending(r => r.Data),
            ("franja", false) => q.OrderByDescending(r => r.FranjaHoraria.Ordre).ThenByDescending(r => r.Data),
            ("creatat", true) => q.OrderBy(r => r.CreatedAt).ThenByDescending(r => r.Data),
            ("creatat", false)=> q.OrderByDescending(r => r.CreatedAt).ThenByDescending(r => r.Data),
            (_, true)  => q.OrderBy(r => r.Data).ThenByDescending(r => r.DegatAt),
            _          => q.OrderByDescending(r => r.Data).ThenByDescending(r => r.DegatAt)
        };

        return await q.Skip((pagina - 1) * midaPagina).Take(midaPagina).ToListAsync(ct);
    }

    public Task<int> GetSessionsCountAsync(
        Guid anyAcademicId,
        Guid? grupId = null, Guid? mestreId = null,
        DateOnly? dataInici = null, DateOnly? dataFi = null,
        Guid? franjaId = null,
        CancellationToken ct = default)
        => _ctx.RegistresAssistencia
            .Where(r => r.Grup.AnyAcademicId == anyAcademicId
                && (grupId == null || r.GrupId == grupId)
                && (mestreId == null || r.MestreId == mestreId)
                && (dataInici == null || r.Data >= dataInici)
                && (dataFi == null || r.Data <= dataFi)
                && (franjaId == null || r.FranjaHorariaId == franjaId))
            .CountAsync(ct);

    public async Task<IReadOnlyList<Application.DTOs.ResumAlumneDto>> GetResumAlumnesAsync(
        Guid anyAcademicId, Guid? grupId = null, Guid? mestreId = null,
        DateOnly? dataInici = null, DateOnly? dataFi = null, Guid? franjaId = null,
        CancellationToken ct = default)
        => await _ctx.AssistenciesAlumnes
            .Where(aa =>
                !aa.RegistreAssistencia.Grup.IsDeleted &&
                aa.RegistreAssistencia.Grup.AnyAcademicId == anyAcademicId &&
                (grupId == null || aa.RegistreAssistencia.GrupId == grupId) &&
                (mestreId == null || aa.RegistreAssistencia.MestreId == mestreId) &&
                (dataInici == null || aa.RegistreAssistencia.Data >= dataInici) &&
                (dataFi == null || aa.RegistreAssistencia.Data <= dataFi) &&
                (franjaId == null || aa.RegistreAssistencia.FranjaHorariaId == franjaId))
            .GroupBy(aa => new
            {
                aa.AlumneId,
                AlumneNom = aa.Alumne.NomComplet,
                GrupNom = aa.RegistreAssistencia.Grup.NomComplet
            })
            .Select(g => new Application.DTOs.ResumAlumneDto
            {
                AlumneId = g.Key.AlumneId,
                AlumneNom = g.Key.AlumneNom,
                GrupNom = g.Key.GrupNom,
                TotalSessions = g.Count(),
                TotalAbsents = g.Sum(aa => aa.Estat == Domain.Entities.EstatAssistencia.Absent ? 1 : 0),
                TotalPresents = g.Sum(aa => aa.Estat == Domain.Entities.EstatAssistencia.Present ? 1 : 0),
                TotalTard = g.Sum(aa => aa.Estat == Domain.Entities.EstatAssistencia.Tard ? 1 : 0)
            })
            .OrderByDescending(r => r.TotalAbsents)
            .ThenBy(r => r.AlumneNom)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Application.DTOs.MestreSenseLlistaDto>> GetMestresSenseLlistaAsync(
        Guid anyAcademicId, CancellationToken ct = default)
    {
        var mestresAmbLlista = await _ctx.RegistresAssistencia
            .Where(r => r.Grup.AnyAcademicId == anyAcademicId)
            .Select(r => r.MestreId)
            .Distinct()
            .ToListAsync(ct);

        return await _ctx.Usuaris
            .Where(u => u.EsActiu && !mestresAmbLlista.Contains(u.Id))
            .OrderBy(u => u.NomComplet)
            .Select(u => new Application.DTOs.MestreSenseLlistaDto
            {
                Id = u.Id,
                NomComplet = u.NomComplet,
                Email = u.Email,
                Rol = u.Rol.ToString()
            })
            .ToListAsync(ct);
    }

    public async Task<Application.DTOs.KpisDashboardDto> GetKpisDashboardAsync(
        Guid anyAcademicId, DateOnly data, CancellationToken ct = default)
    {
        var grups = await _ctx.Grups
            .Where(g => g.AnyAcademicId == anyAcademicId)
            .Select(g => new { g.Id, g.NomComplet })
            .ToListAsync(ct);

        var resum = await _ctx.RegistresAssistencia
            .Where(r => r.Grup.AnyAcademicId == anyAcademicId && r.Data == data)
            .Select(r => new
            {
                r.GrupId,
                Absents = r.Assistencies.Count(a => a.Estat == Domain.Entities.EstatAssistencia.Absent)
            })
            .ToListAsync(ct);

        var grupsAmbLlista = resum.Select(r => r.GrupId).Distinct().ToHashSet();
        var grupsSenseLlista = grups
            .Where(g => !grupsAmbLlista.Contains(g.Id))
            .Select(g => g.NomComplet)
            .OrderBy(n => n)
            .ToList();

        return new Application.DTOs.KpisDashboardDto
        {
            SessionsAvui = resum.Count,
            AbsenciesAvui = resum.Sum(r => r.Absents),
            GrupsTotals = grups.Count,
            GrupsAmbLlista = grupsAmbLlista.Count,
            GrupsSenseLlista = grupsSenseLlista
        };
    }

    public async Task<RegistreAssistencia> AfegirRegistreAsync(
        RegistreAssistencia registre, CancellationToken ct = default)
    {
        await _ctx.RegistresAssistencia.AddAsync(registre, ct);
        return registre;
    }

    public Task ActualitzarRegistreAsync(RegistreAssistencia registre, CancellationToken ct = default)
    {
        _ctx.Entry(registre).State = EntityState.Modified;
        return Task.CompletedTask;
    }

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => _ctx.SaveChangesAsync(ct);
}

// ═══════════════════════════════════════════════════════════
//  REPOSITORI D'USUARIS
// ═══════════════════════════════════════════════════════════

public class UsuariRepository : IUsuariRepository
{
    private readonly DomainCtx _ctx;
    public UsuariRepository(DomainCtx ctx) => _ctx = ctx;

    public Task<Usuari?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _ctx.Usuaris.FirstOrDefaultAsync(u => u.Id == id, ct);

    public Task<Usuari?> GetPerEmailAsync(string email, CancellationToken ct = default)
        => _ctx.Usuaris.FirstOrDefaultAsync(
            u => u.Email.ToLower() == email.ToLower() && u.EsActiu, ct);

    public async Task<IReadOnlyList<Usuari>> GetTotsAsync(CancellationToken ct = default)
        => await _ctx.Usuaris.OrderBy(u => u.Cognom1).ThenBy(u => u.Nom).ToListAsync(ct);

    public async Task<Usuari> AfegirAsync(Usuari usuari, CancellationToken ct = default)
    {
        await _ctx.Usuaris.AddAsync(usuari, ct);
        return usuari;
    }

    public Task ActualitzarAsync(Usuari usuari, CancellationToken ct = default)
    {
        _ctx.Entry(usuari).State = EntityState.Modified;
        return Task.CompletedTask;
    }

    public async Task EsborrarAsync(Guid id, CancellationToken ct = default)
    {
        var usuari = await _ctx.Usuaris.FindAsync(new object[] { id }, ct);
        if (usuari != null)
        {
            usuari.IsDeleted = true;
            usuari.EsActiu = false;
            _ctx.Entry(usuari).State = EntityState.Modified;
        }
    }

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => _ctx.SaveChangesAsync(ct);
}

// ═══════════════════════════════════════════════════════════
//  REPOSITORI DE CALENDARI
// ═══════════════════════════════════════════════════════════

public class CalendariRepository : ICalendariRepository
{
    private readonly DomainCtx _ctx;
    public CalendariRepository(DomainCtx ctx) => _ctx = ctx;

    public Task<AnyAcademic?> GetAnyAcademicActiuAsync(CancellationToken ct = default)
        => _ctx.AnysAcademics
            .Include(a => a.DiesCalendari)
            .FirstOrDefaultAsync(a => a.EsActiu, ct);

    public Task<AnyAcademic?> GetAnyAcademicPerIdAsync(Guid id, CancellationToken ct = default)
        => _ctx.AnysAcademics
            .Include(a => a.DiesCalendari)
            .FirstOrDefaultAsync(a => a.Id == id, ct);

    public async Task<IReadOnlyList<AnyAcademic>> GetAnysAcademicsAsync(CancellationToken ct = default)
        => await _ctx.AnysAcademics
            .OrderByDescending(a => a.DataInici)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<DiaCalendari>> GetDiesRangAsync(
        Guid anyAcademicId, DateOnly dataInici, DateOnly dataFi, CancellationToken ct = default)
        => await _ctx.DiesCalendari
            .Where(d => d.AnyAcademicId == anyAcademicId
                && d.Data >= dataInici && d.Data <= dataFi)
            .OrderBy(d => d.Data)
            .ToListAsync(ct);

    public Task<DiaCalendari?> GetDiaAsync(Guid anyAcademicId, DateOnly data, CancellationToken ct = default)
        => _ctx.DiesCalendari
            .FirstOrDefaultAsync(d => d.AnyAcademicId == anyAcademicId && d.Data == data, ct);

    public async Task<IReadOnlyList<FranjaHoraria>> GetFranjesAsync(CancellationToken ct = default)
        => await _ctx.FranjesHoraries.OrderBy(f => f.Ordre).ToListAsync(ct);

    public async Task AfegirDiesAsync(IEnumerable<DiaCalendari> dies, CancellationToken ct = default)
        => await _ctx.DiesCalendari.AddRangeAsync(dies, ct);

    public Task ActualitzarDiaAsync(DiaCalendari dia, CancellationToken ct = default)
    {
        _ctx.Entry(dia).State = EntityState.Modified;
        return Task.CompletedTask;
    }

    public async Task EsborrarDiaAsync(Guid anyAcademicId, DateOnly data, CancellationToken ct = default)
    {
        var dia = await _ctx.DiesCalendari.FirstOrDefaultAsync(
            d => d.AnyAcademicId == anyAcademicId && d.Data == data, ct);
        if (dia != null) _ctx.DiesCalendari.Remove(dia);
    }

    public async Task<AnyAcademic> AfegirAnyAcademicAsync(AnyAcademic any, CancellationToken ct = default)
    {
        await _ctx.AnysAcademics.AddAsync(any, ct);
        return any;
    }

    public Task ActualitzarAnyAcademicAsync(AnyAcademic any, CancellationToken ct = default)
    {
        _ctx.Entry(any).State = EntityState.Modified;
        return Task.CompletedTask;
    }

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => _ctx.SaveChangesAsync(ct);
}
