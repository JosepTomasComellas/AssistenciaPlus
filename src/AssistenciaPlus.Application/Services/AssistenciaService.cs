using AssistenciaPlus.Application.DTOs;
using AssistenciaPlus.Application.Interfaces;
using AssistenciaPlus.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace AssistenciaPlus.Application.Services;

public class AssistenciaService : IAssistenciaService
{
    private readonly IAssistenciaRepository _assistenciaRepo;
    private readonly IGrupRepository _grupRepo;
    private readonly IAlumneRepository _alumneRepo;
    private readonly ICalendariRepository _calendariRepo;
    private readonly ILogger<AssistenciaService> _logger;

    public AssistenciaService(
        IAssistenciaRepository assistenciaRepo,
        IGrupRepository grupRepo,
        IAlumneRepository alumneRepo,
        ICalendariRepository calendariRepo,
        ILogger<AssistenciaService> logger)
    {
        _assistenciaRepo = assistenciaRepo;
        _grupRepo = grupRepo;
        _alumneRepo = alumneRepo;
        _calendariRepo = calendariRepo;
        _logger = logger;
    }

    public async Task<RegistreAssistenciaDto> DesarSessioCompletaAsync(
        DesarSessioDto dto, Guid mestreId, CancellationToken ct = default)
    {
        var registreExistent = await _assistenciaRepo.GetRegistreAmbAssistenciesAsync(
            dto.GrupId, dto.FranjaHorariaId, dto.Data, ct);

        RegistreAssistencia registre;

        if (registreExistent != null)
        {
            registre = registreExistent;
            registre.Observacio = dto.Observacio;
            registre.DegatAt = DateTime.UtcNow;

            foreach (var inputAssist in dto.Assistencies)
            {
                var assistExistent = registre.Assistencies
                    .FirstOrDefault(a => a.AlumneId == inputAssist.AlumneId);

                if (assistExistent != null)
                {
                    assistExistent.Estat = inputAssist.Estat;
                    assistExistent.Motiu = inputAssist.Motiu;
                    assistExistent.Torna = inputAssist.Torna;
                    assistExistent.MinutsRetard = inputAssist.MinutsRetard;
                    assistExistent.Observacio = inputAssist.Observacio;
                }
                else
                {
                    registre.Assistencies.Add(new AssistenciaAlumne
                    {
                        RegistreAssistenciaId = registre.Id,
                        AlumneId = inputAssist.AlumneId,
                        Estat = inputAssist.Estat,
                        Motiu = inputAssist.Motiu,
                        Torna = inputAssist.Torna,
                        MinutsRetard = inputAssist.MinutsRetard,
                        Observacio = inputAssist.Observacio
                    });
                }
            }

            await _assistenciaRepo.ActualitzarRegistreAsync(registre, ct);
        }
        else
        {
            registre = new RegistreAssistencia
            {
                GrupId = dto.GrupId,
                FranjaHorariaId = dto.FranjaHorariaId,
                Data = dto.Data,
                MestreId = mestreId,
                Observacio = dto.Observacio,
                DegatAt = DateTime.UtcNow,
                Assistencies = dto.Assistencies.Select(a => new AssistenciaAlumne
                {
                    AlumneId = a.AlumneId,
                    Estat = a.Estat,
                    Motiu = a.Motiu,
                    Torna = a.Torna,
                    MinutsRetard = a.MinutsRetard,
                    Observacio = a.Observacio
                }).ToList()
            };

            await _assistenciaRepo.AfegirRegistreAsync(registre, ct);
        }

        // Aplicar absència a la resta del dia als alumnes marcats
        var alumnesToAplicar = dto.Assistencies
            .Where(a => a.AplicarRestaDia && a.Estat == EstatAssistencia.AbsenciaParcial)
            .ToList();

        if (alumnesToAplicar.Any())
        {
            var franjes = await _calendariRepo.GetFranjesAsync(ct);
            var franjaActual = franjes.FirstOrDefault(f => f.Id == dto.FranjaHorariaId);

            if (franjaActual != null)
            {
                var franjesRestants = franjes
                    .Where(f => f.Ordre > franjaActual.Ordre)
                    .OrderBy(f => f.Ordre)
                    .ToList();

                foreach (var alumneInput in alumnesToAplicar)
                {
                    foreach (var franja in franjesRestants)
                    {
                        await AplicarAbsenciaFranjaAsync(
                            alumneInput.AlumneId, dto.GrupId, dto.Data,
                            franja.Id, alumneInput.Motiu ?? MotiuAbsencia.SenseMotiu,
                            alumneInput.Torna ?? false, mestreId, ct);
                    }
                }
            }
        }

        await _assistenciaRepo.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Sessió desada: Grup {GrupId}, Franja {FranjaId}, Data {Data}, {Count} alumnes",
            dto.GrupId, dto.FranjaHorariaId, dto.Data, dto.Assistencies.Count);

        return await MaparRegistreAsync(registre, ct);
    }

    public async Task AplicarAbsenciaParcialRestaDiaAsync(
        AplicarAbsenciaParcialDto dto, Guid mestreId, CancellationToken ct = default)
    {
        var franjes = await _calendariRepo.GetFranjesAsync(ct);
        var franjaActual = franjes.FirstOrDefault(f => f.Id == dto.FranjaActualId)
            ?? throw new KeyNotFoundException("Franja horària no trobada");

        var franjesRestants = franjes
            .Where(f => f.Ordre > franjaActual.Ordre)
            .OrderBy(f => f.Ordre)
            .ToList();

        foreach (var franja in franjesRestants)
        {
            await AplicarAbsenciaFranjaAsync(
                dto.AlumneId, dto.GrupId, dto.Data,
                franja.Id, dto.Motiu, dto.Torna, mestreId, ct);
        }

        await _assistenciaRepo.SaveChangesAsync(ct);
    }

    public async Task<decimal> CalcularPercentatgeAbsenciaAsync(
        Guid alumneId, Guid anyAcademicId, DateOnly dataInici, DateOnly dataFi,
        CancellationToken ct = default)
    {
        var dies = await _calendariRepo.GetDiesRangAsync(anyAcademicId, dataInici, dataFi, ct);
        var franjes = await _calendariRepo.GetFranjesAsync(ct);

        var totalFrangesPossibles = dies
            .Where(d => d.TipusDia is TipusDia.Lectiu or TipusDia.JornadaIntensiva)
            .Sum(d => d.TipusDia == TipusDia.JornadaIntensiva
                ? franjes.Count(f => f.EsJornadaIntensiva)
                : franjes.Count);

        if (totalFrangesPossibles == 0) return 0;

        var assistencies = await _assistenciaRepo.GetAssistenciesAlumneAsync(alumneId, dataInici, dataFi, ct);

        var frangesNoPresents = assistencies.Count(a =>
            a.Estat is EstatAssistencia.Absent or EstatAssistencia.Tard or EstatAssistencia.AbsenciaParcial);

        return Math.Round((decimal)frangesNoPresents / totalFrangesPossibles * 100, 2);
    }

    public async Task<RegistreAssistenciaDto?> GetRegistreAsync(
        Guid grupId, Guid franjaId, DateOnly data, CancellationToken ct = default)
    {
        var registre = await _assistenciaRepo.GetRegistreAmbAssistenciesAsync(grupId, franjaId, data, ct);
        if (registre == null) return null;
        return await MaparRegistreAsync(registre, ct);
    }

    public async Task<IReadOnlyList<RegistreAssistenciaDto>> GetRegistresPerDataAsync(
        Guid grupId, DateOnly data, CancellationToken ct = default)
    {
        var registres = await _assistenciaRepo.GetRegistresPerGrupIDataAsync(grupId, data, ct);
        var result = new List<RegistreAssistenciaDto>();
        foreach (var r in registres)
            result.Add(await MaparRegistreAsync(r, ct));
        return result;
    }

    // ── Helpers ─────────────────────────────────────────────

    private async Task AplicarAbsenciaFranjaAsync(
        Guid alumneId, Guid grupId, DateOnly data, Guid franjaId,
        MotiuAbsencia motiu, bool torna, Guid mestreId, CancellationToken ct)
    {
        var registre = await _assistenciaRepo.GetRegistreAmbAssistenciesAsync(grupId, franjaId, data, ct);

        if (registre == null)
        {
            registre = new RegistreAssistencia
            {
                GrupId = grupId,
                FranjaHorariaId = franjaId,
                Data = data,
                MestreId = mestreId,
                DegatAt = DateTime.UtcNow,
                Assistencies = [new AssistenciaAlumne
                {
                    AlumneId = alumneId,
                    Estat = EstatAssistencia.AbsenciaParcial,
                    Motiu = motiu,
                    Torna = torna,
                    AplicatRestaDia = true
                }]
            };
            await _assistenciaRepo.AfegirRegistreAsync(registre, ct);
        }
        else
        {
            var assist = registre.Assistencies.FirstOrDefault(a => a.AlumneId == alumneId);
            if (assist != null)
            {
                assist.Estat = EstatAssistencia.AbsenciaParcial;
                assist.Motiu = motiu;
                assist.Torna = torna;
                assist.AplicatRestaDia = true;
            }
            else
            {
                registre.Assistencies.Add(new AssistenciaAlumne
                {
                    RegistreAssistenciaId = registre.Id,
                    AlumneId = alumneId,
                    Estat = EstatAssistencia.AbsenciaParcial,
                    Motiu = motiu,
                    Torna = torna,
                    AplicatRestaDia = true
                });
            }
            await _assistenciaRepo.ActualitzarRegistreAsync(registre, ct);
        }
    }

    private async Task<RegistreAssistenciaDto> MaparRegistreAsync(
        RegistreAssistencia r, CancellationToken ct)
    {
        // Les navegacions poden estar carregades o no; fem servir valors ja disponibles
        return new RegistreAssistenciaDto
        {
            Id = r.Id,
            GrupId = r.GrupId,
            GrupNom = r.Grup?.NomComplet ?? string.Empty,
            FranjaHorariaId = r.FranjaHorariaId,
            FranjaNom = r.FranjaHoraria?.Nom ?? string.Empty,
            Data = r.Data,
            MestreId = r.MestreId,
            MestreNom = r.Mestre?.NomComplet ?? string.Empty,
            Observacio = r.Observacio,
            DegatAt = r.DegatAt,
            Assistencies = r.Assistencies.OrderBy(a => a.Alumne?.OrdreFusteta ?? 0)
                .Select(a => new AssistenciaAlumneDto
                {
                    Id = a.Id,
                    AlumneId = a.AlumneId,
                    AlumneNomComplet = a.Alumne?.NomComplet ?? string.Empty,
                    AlumneNomFusteta = a.Alumne?.NomFusteta ?? string.Empty,
                    OrdreFusteta = a.Alumne?.OrdreFusteta ?? 0,
                    Estat = a.Estat,
                    Motiu = a.Motiu,
                    Torna = a.Torna,
                    MinutsRetard = a.MinutsRetard,
                    Observacio = a.Observacio,
                    AplicatRestaDia = a.AplicatRestaDia
                }).ToList()
        };
    }
}
