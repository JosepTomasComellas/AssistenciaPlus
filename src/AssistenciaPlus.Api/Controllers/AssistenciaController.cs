using AssistenciaPlus.Application.Common;
using AssistenciaPlus.Application.DTOs;
using AssistenciaPlus.Application.Interfaces;
using AssistenciaPlus.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace AssistenciaPlus.Api.Controllers;

[ApiController]
[Route("api/assistencia")]
[Authorize]
public class AssistenciaController : BaseApiController
{
    private readonly IAssistenciaService _assistenciaService;
    private readonly IAssistenciaRepository _assistenciaRepo;
    private readonly IGrupRepository _grupRepo;
    private readonly IHubContext<Middleware.AttendanceHub> _hub;
    private readonly ILogger<AssistenciaController> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public AssistenciaController(
        IAssistenciaService assistenciaService,
        IAssistenciaRepository assistenciaRepo,
        IGrupRepository grupRepo,
        IHubContext<Middleware.AttendanceHub> hub,
        ILogger<AssistenciaController> logger,
        IServiceScopeFactory scopeFactory)
    {
        _assistenciaService = assistenciaService;
        _assistenciaRepo = assistenciaRepo;
        _grupRepo = grupRepo;
        _hub = hub;
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    /// <summary>
    /// Retorna tots els registres d'un grup per a una data concreta.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<RegistreAssistenciaDto>>>> GetRegistres(
        [FromQuery] Guid grupId, [FromQuery] DateOnly data, CancellationToken ct)
    {
        if (grupId == Guid.Empty)
            return BadRequest(ApiResponse<IEnumerable<RegistreAssistenciaDto>>.Fail("Cal indicar el grupId"));

        var registres = await _assistenciaService.GetRegistresPerDataAsync(grupId, data, ct);
        return Ok(ApiResponse<IEnumerable<RegistreAssistenciaDto>>.Ok(registres));
    }

    /// <summary>
    /// Retorna el registre d'assistència d'un grup en una franja i data concreta.
    /// </summary>
    [HttpGet("{grupId:guid}/{franjaId:guid}/{data}")]
    public async Task<ActionResult<ApiResponse<RegistreAssistenciaDto>>> GetRegistre(
        Guid grupId, Guid franjaId, DateOnly data, CancellationToken ct)
    {
        var registre = await _assistenciaService.GetRegistreAsync(grupId, franjaId, data, ct);
        if (registre == null)
            return NotFound(ApiResponse<RegistreAssistenciaDto>.Fail("Registre no trobat"));

        return Ok(ApiResponse<RegistreAssistenciaDto>.Ok(registre));
    }

    /// <summary>
    /// Desa una sessió completa d'assistència (crea o actualitza).
    /// Tots els estats de tots els alumnes del grup es passen d'una vegada.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<RegistreAssistenciaDto>>> DesarSessio(
        [FromBody] DesarSessioDto dto, CancellationToken ct)
    {
        var mestreId = GetCurrentUserId();
        var rol = User.FindFirst(ClaimTypes.Role)?.Value;

        if (rol == nameof(RolUsuari.Administratiu))
            return Forbid();

        if (rol == nameof(RolUsuari.Mestre))
        {
            var grup = await _grupRepo.GetByIdAsync(dto.GrupId, ct);
            if (grup == null) return NotFound(ApiResponse<RegistreAssistenciaDto>.Fail("Grup no trobat"));
            var esAutoritzat = grup.TutorId == mestreId
                || await _grupRepo.EsMestreAutoritzatAsync(dto.GrupId, mestreId, ct);
            if (!esAutoritzat) return Forbid();
        }

        var registre = await _assistenciaService.DesarSessioCompletaAsync(dto, mestreId, ct);

        // Notificació en temps real — assistència actualitzada
        await _hub.Clients.Group($"group:{dto.GrupId}")
            .SendAsync("AssistenciaActualitzada",
                dto.FranjaHorariaId.ToString(),
                dto.GrupId.ToString(),
                ct);

        // Comprovació d'alertes en segon pla (no bloqueja la resposta, CT propi)
        _ = EmitreAlertesAbsenciaAsync(dto, CancellationToken.None);

        return Ok(ApiResponse<RegistreAssistenciaDto>.Ok(registre));
    }

    private async Task EmitreAlertesAbsenciaAsync(DesarSessioDto dto, CancellationToken ct)
    {
        try
        {
            var absentsSession = dto.Assistencies
                .Where(a => a.Estat == EstatAssistencia.Absent)
                .Select(a => a.AlumneId)
                .ToList();

            if (absentsSession.Count == 0) return;

            // Scope propi per evitar usar el DbContext ja disposat del request original
            await using var scope = _scopeFactory.CreateAsyncScope();
            var grupRepo = scope.ServiceProvider.GetRequiredService<IGrupRepository>();
            var assistenciaRepo = scope.ServiceProvider.GetRequiredService<IAssistenciaRepository>();

            var grup = await grupRepo.GetByIdAmbDetallsAsync(dto.GrupId, ct);
            if (grup == null) return;

            var inicMes = new DateOnly(dto.Data.Year, dto.Data.Month, 1);
            var fiMes = inicMes.AddMonths(1).AddDays(-1);

            foreach (var alumneId in absentsSession)
            {
                var assistencies = await assistenciaRepo.GetAssistenciesAlumneAsync(
                    alumneId, inicMes, fiMes, ct);

                var totalAbsents = assistencies.Count(a =>
                    a.Estat == Domain.Entities.EstatAssistencia.Absent);

                if (totalAbsents < 3) continue;

                var alumne = grup.Alumnes.FirstOrDefault(a => a.Id == alumneId);
                if (alumne == null) continue;

                var esCritic = totalAbsents >= 6;
                var missatge = esCritic
                    ? $"Situació crítica: {totalAbsents} absències aquest mes"
                    : $"En alerta: {totalAbsents} absències aquest mes";

                await _hub.Clients.All.SendAsync("AlertaAbsencia",
                    alumneId.ToString(),
                    alumne.NomComplet,
                    grup.NomComplet,
                    missatge,
                    esCritic,
                    ct);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error en emetre alertes d'absència per grup {GrupId}", dto.GrupId);
        }
    }

    /// <summary>
    /// Aplica una absència parcial a totes les franges restants del dia.
    /// </summary>
    [HttpPost("absencia-parcial")]
    public async Task<ActionResult<ApiResponse>> AplicarAbsenciaParcial(
        [FromBody] AplicarAbsenciaParcialDto dto, CancellationToken ct)
    {
        var mestreId = GetCurrentUserId();
        var rol = User.FindFirst(ClaimTypes.Role)?.Value;

        if (rol == nameof(RolUsuari.Administratiu))
            return Forbid();

        if (rol == nameof(RolUsuari.Mestre))
        {
            var grup = await _grupRepo.GetByIdAsync(dto.GrupId, ct);
            if (grup == null) return NotFound(ApiResponse.Fail("Grup no trobat"));
            var esAutoritzat = grup.TutorId == mestreId
                || await _grupRepo.EsMestreAutoritzatAsync(dto.GrupId, mestreId, ct);
            if (!esAutoritzat) return Forbid();
        }

        await _assistenciaService.AplicarAbsenciaParcialRestaDiaAsync(dto, mestreId, ct);
        return Ok(ApiResponse.Ok());
    }

    /// <summary>
    /// Llista les sessions d'assistència de l'any acadèmic actiu (traçabilitat).
    /// Accés restringit a EquipDirectiu.
    /// </summary>
    [HttpGet("sessions")]
    [Authorize(Roles = "EquipDirectiu")]
    public async Task<ActionResult<ApiResponse<object>>> GetSessions(
        [FromQuery] Guid anyAcademicId,
        [FromQuery] int pagina = 1,
        [FromQuery] int midaPagina = 50,
        [FromQuery] Guid? grupId = null,
        [FromQuery] Guid? mestreId = null,
        [FromQuery] DateOnly? dataInici = null,
        [FromQuery] DateOnly? dataFi = null,
        [FromQuery] Guid? franjaId = null,
        CancellationToken ct = default)
    {
        midaPagina = Math.Clamp(midaPagina, 1, 200);
        pagina = Math.Max(1, pagina);

        var sessions = await _assistenciaRepo.GetSessionsAsync(
            anyAcademicId, pagina, midaPagina,
            grupId, mestreId, dataInici, dataFi, franjaId, ct);
        var total = await _assistenciaRepo.GetSessionsCountAsync(
            anyAcademicId, grupId, mestreId, dataInici, dataFi, franjaId, ct);

        var dtos = sessions.Select(r => new AssistenciaPlus.Application.DTOs.SessioTraçabilitatDto
        {
            Id = r.Id,
            Data = r.Data,
            GrupNom = r.Grup?.NomComplet ?? string.Empty,
            FranjaNom = r.FranjaHoraria?.Nom ?? string.Empty,
            MestreNomComplet = r.Mestre?.NomComplet ?? string.Empty,
            NombreAbsents = r.Assistencies.Count(a => a.Estat == Domain.Entities.EstatAssistencia.Absent),
            NombrePresents = r.Assistencies.Count(a => a.Estat == Domain.Entities.EstatAssistencia.Present),
            NombreTard = r.Assistencies.Count(a => a.Estat == Domain.Entities.EstatAssistencia.Tard),
            NombreTotal = r.Assistencies.Count,
            DegatAt = r.DegatAt
        });

        return Ok(ApiResponse<object>.Ok(new { Total = total, Sessions = dtos }));
    }

    /// <summary>
    /// Calcula el percentatge d'absència d'un alumne en un rang de dates.
    /// </summary>
    [HttpGet("percentatge/{alumneId:guid}")]
    public async Task<ActionResult<ApiResponse<decimal>>> GetPercentatgeAbsencia(
        Guid alumneId,
        [FromQuery] Guid anyAcademicId,
        [FromQuery] DateOnly dataInici,
        [FromQuery] DateOnly dataFi,
        CancellationToken ct)
    {
        var percentatge = await _assistenciaService.CalcularPercentatgeAbsenciaAsync(
            alumneId, anyAcademicId, dataInici, dataFi, ct);

        return Ok(ApiResponse<decimal>.Ok(percentatge));
    }
}
