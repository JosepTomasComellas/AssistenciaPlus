using AssistenciaPlus.Application.DTOs;
using AssistenciaPlus.Application.Interfaces;
using AssistenciaPlus.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace AssistenciaPlus.Application.Services;

public class InformesService : IInformesService
{
    private readonly IAssistenciaRepository _assistenciaRepo;
    private readonly IGrupRepository _grupRepo;
    private readonly IAlumneRepository _alumneRepo;
    private readonly ICalendariRepository _calendariRepo;
    private readonly ILogger<InformesService> _logger;

    public InformesService(
        IAssistenciaRepository assistenciaRepo,
        IGrupRepository grupRepo,
        IAlumneRepository alumneRepo,
        ICalendariRepository calendariRepo,
        ILogger<InformesService> logger)
    {
        _assistenciaRepo = assistenciaRepo;
        _grupRepo = grupRepo;
        _alumneRepo = alumneRepo;
        _calendariRepo = calendariRepo;
        _logger = logger;
    }

    public async Task<InformeMensualDto> GenerarInformeMensualGrupAsync(
        Guid grupId, int any, int mes, CancellationToken ct = default)
    {
        var grup = await _grupRepo.GetByIdAmbDetallsAsync(grupId, ct)
            ?? throw new KeyNotFoundException($"Grup {grupId} no trobat");

        var anyAcademic = await _calendariRepo.GetAnyAcademicPerIdAsync(grup.AnyAcademicId, ct)
            ?? throw new KeyNotFoundException("Any acadèmic no trobat");

        var (dataInici, dataFi) = GetRangMes(any, mes);
        var alumnes = await _alumneRepo.GetPerGrupAsync(grupId, ct);

        var informe = await GenerarInformeMensualInteriorAsync(
            alumnes, anyAcademic, dataInici, dataFi, ct);

        return informe with
        {
            AnyAcademic = anyAcademic.Nom,
            Any = any,
            Mes = mes,
            GrupNom = grup.NomComplet,
            TotalAlumnes = alumnes.Count
        };
    }

    public async Task<InformeMensualDto> GenerarInformeMensualCicleAsync(
        Guid cicleId, int any, int mes, CancellationToken ct = default)
    {
        var anyActiu = await _calendariRepo.GetAnyAcademicActiuAsync(ct)
            ?? throw new InvalidOperationException("No hi ha cap any acadèmic actiu");

        var grups = await _grupRepo.GetPerCicleAsync(cicleId, anyActiu.Id, ct);
        var cicles = await _grupRepo.GetCiclesAsync(ct);
        var cicle = cicles.FirstOrDefault(c => c.Id == cicleId);

        var (dataInici, dataFi) = GetRangMes(any, mes);

        var totsAlumnes = new List<Alumne>();
        foreach (var grup in grups)
            totsAlumnes.AddRange(await _alumneRepo.GetPerGrupAsync(grup.Id, ct));

        var informe = await GenerarInformeMensualInteriorAsync(
            totsAlumnes, anyActiu, dataInici, dataFi, ct);

        return informe with
        {
            AnyAcademic = anyActiu.Nom,
            Any = any,
            Mes = mes,
            CicleNom = cicle?.Nom,
            TotalAlumnes = totsAlumnes.Count
        };
    }

    public async Task<InformeTrimestralDto> GenerarInformeTrimestralGrupAsync(
        Guid grupId, int trimestre, Guid anyAcademicId, CancellationToken ct = default)
    {
        var grup = await _grupRepo.GetByIdAmbDetallsAsync(grupId, ct)
            ?? throw new KeyNotFoundException($"Grup {grupId} no trobat");

        var anyAcademic = await _calendariRepo.GetAnyAcademicPerIdAsync(anyAcademicId, ct)
            ?? throw new KeyNotFoundException("Any acadèmic no trobat");

        var (dataInici, dataFi) = GetRangTrimestre(anyAcademic, trimestre);
        var alumnes = await _alumneRepo.GetPerGrupAsync(grupId, ct);

        var informe = await GenerarInformeTrimestralInteriorAsync(
            alumnes, anyAcademic, dataInici, dataFi, trimestre, ct);

        return informe with
        {
            GrupNom = grup.NomComplet,
            TotalAlumnes = alumnes.Count
        };
    }

    public async Task<InformeTrimestralDto> GenerarInformeTrimestralCicleAsync(
        Guid cicleId, int trimestre, Guid anyAcademicId, CancellationToken ct = default)
    {
        var anyAcademic = await _calendariRepo.GetAnyAcademicPerIdAsync(anyAcademicId, ct)
            ?? throw new KeyNotFoundException("Any acadèmic no trobat");

        var grups = await _grupRepo.GetPerCicleAsync(cicleId, anyAcademicId, ct);
        var cicles = await _grupRepo.GetCiclesAsync(ct);
        var cicle = cicles.FirstOrDefault(c => c.Id == cicleId);

        var (dataInici, dataFi) = GetRangTrimestre(anyAcademic, trimestre);

        var totsAlumnes = new List<Alumne>();
        foreach (var grup in grups)
            totsAlumnes.AddRange(await _alumneRepo.GetPerGrupAsync(grup.Id, ct));

        var informe = await GenerarInformeTrimestralInteriorAsync(
            totsAlumnes, anyAcademic, dataInici, dataFi, trimestre, ct);

        return informe with
        {
            CicleNom = cicle?.Nom,
            TotalAlumnes = totsAlumnes.Count
        };
    }

    // ── Helpers ─────────────────────────────────────────────

    private async Task<InformeMensualDto> GenerarInformeMensualInteriorAsync(
        IReadOnlyList<Alumne> alumnes, AnyAcademic anyAcademic,
        DateOnly dataInici, DateOnly dataFi, CancellationToken ct)
    {
        var dies = await _calendariRepo.GetDiesRangAsync(anyAcademic.Id, dataInici, dataFi, ct);
        var franjes = await _calendariRepo.GetFranjesAsync(ct);

        var totalFrangesPossibles = CalcularFrangesPossibles(dies, franjes);

        var alumnesInforme = new List<InformeAlumneDto>();

        foreach (var alumne in alumnes.Where(a => a.EsActiu))
        {
            var assistencies = await _assistenciaRepo.GetAssistenciesAlumneAsync(
                alumne.Id, dataInici, dataFi, ct);

            var dto = CalcularInformeAlumne(alumne, assistencies, totalFrangesPossibles, false);
            alumnesInforme.Add(dto);
        }

        return new InformeMensualDto
        {
            Alumnes = alumnesInforme.OrderBy(a => a.AlumneNom).ToList()
        };
    }

    private async Task<InformeTrimestralDto> GenerarInformeTrimestralInteriorAsync(
        IReadOnlyList<Alumne> alumnes, AnyAcademic anyAcademic,
        DateOnly dataInici, DateOnly dataFi, int trimestre, CancellationToken ct)
    {
        var dies = await _calendariRepo.GetDiesRangAsync(anyAcademic.Id, dataInici, dataFi, ct);
        var franjes = await _calendariRepo.GetFranjesAsync(ct);

        var totalFrangesPossibles = CalcularFrangesPossibles(dies, franjes);

        var alumnesInforme = new List<InformeAlumneDto>();

        foreach (var alumne in alumnes.Where(a => a.EsActiu))
        {
            var assistencies = await _assistenciaRepo.GetAssistenciesAlumneAsync(
                alumne.Id, dataInici, dataFi, ct);

            var dto = CalcularInformeAlumne(alumne, assistencies, totalFrangesPossibles, true);
            alumnesInforme.Add(dto);
        }

        return new InformeTrimestralDto
        {
            AnyAcademic = anyAcademic.Nom,
            Trimestre = trimestre,
            DataInici = dataInici,
            DataFi = dataFi,
            Alumnes = alumnesInforme.OrderBy(a => a.AlumneNom).ToList()
        };
    }

    private static InformeAlumneDto CalcularInformeAlumne(
        Alumne alumne,
        IReadOnlyList<AssistenciaAlumne> assistencies,
        int totalFrangesPossibles,
        bool esTrimestral)
    {
        var absents = assistencies.Count(a => a.Estat == EstatAssistencia.Absent);
        var tard = assistencies.Count(a => a.Estat == EstatAssistencia.Tard);
        var parcials = assistencies.Count(a => a.Estat == EstatAssistencia.AbsenciaParcial);
        var presents = assistencies.Count(a => a.Estat == EstatAssistencia.Present);

        var frangesNoPresents = absents + tard + parcials;
        var percentatge = totalFrangesPossibles > 0
            ? Math.Round((decimal)frangesNoPresents / totalFrangesPossibles * 100, 2)
            : 0;

        // Llindar crític: 25% mensual, 20% trimestral
        var llindarcCritic = esTrimestral ? 20m : 25m;

        var motius = assistencies
            .Where(a => a.Motiu.HasValue)
            .GroupBy(a => a.Motiu!.Value.ToString())
            .ToDictionary(g => g.Key, g => g.Count());

        return new InformeAlumneDto
        {
            AlumneId = alumne.Id,
            AlumneNom = alumne.NomComplet,
            GrupNom = alumne.Grup?.NomComplet ?? string.Empty,
            TotalFrangesPossibles = totalFrangesPossibles,
            FrangesPresents = presents,
            FrangesAbsents = absents,
            FrangesTard = tard,
            FrangesAbsenciaParcial = parcials,
            PercentatgeAbsencia = percentatge,
            EsAlerta = percentatge >= 10m,
            EsCritic = percentatge >= llindarcCritic,
            DetallMotius = motius
        };
    }

    private static int CalcularFrangesPossibles(
        IReadOnlyList<DiaCalendari> dies, IReadOnlyList<FranjaHoraria> franjes)
    {
        var totalFranjesJI = franjes.Count(f => f.EsJornadaIntensiva);
        var totalFranjesDia = franjes.Count;

        return dies
            .Where(d => d.TipusDia is TipusDia.Lectiu or TipusDia.JornadaIntensiva)
            .Sum(d => d.TipusDia == TipusDia.JornadaIntensiva ? totalFranjesJI : totalFranjesDia);
    }

    private static (DateOnly inici, DateOnly fi) GetRangMes(int any, int mes)
    {
        var inici = new DateOnly(any, mes, 1);
        var fi = inici.AddMonths(1).AddDays(-1);
        return (inici, fi);
    }

    private static (DateOnly inici, DateOnly fi) GetRangTrimestre(AnyAcademic anyAcademic, int trimestre)
        => trimestre switch
        {
            1 => (anyAcademic.IniciT1, anyAcademic.FiT1),
            2 => (anyAcademic.IniciT2, anyAcademic.FiT2),
            3 => (anyAcademic.IniciT3, anyAcademic.FiT3),
            _ => throw new ArgumentException($"Trimestre invàlid: {trimestre}. Ha de ser 1, 2 o 3.")
        };
}
