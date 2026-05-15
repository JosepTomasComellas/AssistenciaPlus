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

    public AssistenciaController(
        IAssistenciaService assistenciaService,
        IAssistenciaRepository assistenciaRepo,
        IGrupRepository grupRepo,
        IHubContext<Middleware.AttendanceHub> hub,
        ILogger<AssistenciaController> logger)
    {
        _assistenciaService = assistenciaService;
        _assistenciaRepo = assistenciaRepo;
        _grupRepo = grupRepo;
        _hub = hub;
        _logger = logger;
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
            if (grup.TutorId != mestreId) return Forbid();
        }

        var registre = await _assistenciaService.DesarSessioCompletaAsync(dto, mestreId, ct);

        // Notificació en temps real a l'equip directiu
        await _hub.Clients.Group($"group:{dto.GrupId}")
            .SendAsync("AssistenciaActualitzada", new
            {
                grupId = dto.GrupId,
                franjaId = dto.FranjaHorariaId,
                data = dto.Data.ToString("yyyy-MM-dd")
            }, ct);

        return Ok(ApiResponse<RegistreAssistenciaDto>.Ok(registre));
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
            if (grup.TutorId != mestreId) return Forbid();
        }

        await _assistenciaService.AplicarAbsenciaParcialRestaDiaAsync(dto, mestreId, ct);
        return Ok(ApiResponse.Ok());
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
