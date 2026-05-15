using AssistenciaPlus.Application.Interfaces;
using AssistenciaPlus.Domain.Entities;
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
            .Where(g => g.TutorId == mestreId && g.AnyAcademicId == anyAcademicId)
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
