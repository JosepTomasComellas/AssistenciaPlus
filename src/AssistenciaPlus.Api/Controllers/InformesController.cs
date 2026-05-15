using AssistenciaPlus.Application.Common;
using AssistenciaPlus.Application.DTOs;
using AssistenciaPlus.Application.Interfaces;
using AssistenciaPlus.Core.Interfaces;
using AssistenciaPlus.Infrastructure.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace AssistenciaPlus.Api.Controllers;

[ApiController]
[Route("api/informes")]
[Authorize(Policy = "NomesEquipDirectiuIAdministratiu")]
public class InformesController : ControllerBase
{
    private readonly IInformesService _informesService;
    private readonly IEmailService _emailService;
    private readonly InformesExcelService _excelService;
    private readonly IConfiguration _config;
    private readonly ILogger<InformesController> _logger;

    public InformesController(
        IInformesService informesService,
        IEmailService emailService,
        InformesExcelService excelService,
        IConfiguration config,
        ILogger<InformesController> logger)
    {
        _informesService = informesService;
        _emailService = emailService;
        _excelService = excelService;
        _config = config;
        _logger = logger;
    }

    /// <summary>Genera l'informe mensual d'assistència per grup.</summary>
    [HttpGet("mensual/grup/{grupId:guid}")]
    public async Task<ActionResult<ApiResponse<InformeMensualDto>>> GetInformeMensualGrup(
        Guid grupId, [FromQuery] int any, [FromQuery] int mes, CancellationToken ct)
    {
        if (mes < 1 || mes > 12)
            return BadRequest(ApiResponse<InformeMensualDto>.Fail("El mes ha de ser entre 1 i 12"));
        if (any < 2000 || any > DateTime.UtcNow.Year + 1)
            return BadRequest(ApiResponse<InformeMensualDto>.Fail("Any fora de rang"));

        var informe = await _informesService.GenerarInformeMensualGrupAsync(grupId, any, mes, ct);
        return Ok(ApiResponse<InformeMensualDto>.Ok(informe));
    }

    /// <summary>Genera l'informe mensual d'assistència per cicle.</summary>
    [HttpGet("mensual/cicle/{cicleId:guid}")]
    public async Task<ActionResult<ApiResponse<InformeMensualDto>>> GetInformeMensualCicle(
        Guid cicleId, [FromQuery] int any, [FromQuery] int mes, CancellationToken ct)
    {
        if (mes < 1 || mes > 12)
            return BadRequest(ApiResponse<InformeMensualDto>.Fail("El mes ha de ser entre 1 i 12"));
        if (any < 2000 || any > DateTime.UtcNow.Year + 1)
            return BadRequest(ApiResponse<InformeMensualDto>.Fail("Any fora de rang"));

        var informe = await _informesService.GenerarInformeMensualCicleAsync(cicleId, any, mes, ct);
        return Ok(ApiResponse<InformeMensualDto>.Ok(informe));
    }

    /// <summary>Genera l'informe trimestral d'assistència per grup.</summary>
    [HttpGet("trimestral/grup/{grupId:guid}")]
    public async Task<ActionResult<ApiResponse<InformeTrimestralDto>>> GetInformeTrimestralGrup(
        Guid grupId, [FromQuery] int trimestre, [FromQuery] Guid anyAcademicId, CancellationToken ct)
    {
        if (trimestre < 1 || trimestre > 3)
            return BadRequest(ApiResponse<InformeTrimestralDto>.Fail("El trimestre ha de ser 1, 2 o 3"));

        var informe = await _informesService.GenerarInformeTrimestralGrupAsync(
            grupId, trimestre, anyAcademicId, ct);
        return Ok(ApiResponse<InformeTrimestralDto>.Ok(informe));
    }

    /// <summary>Genera l'informe trimestral d'assistència per cicle.</summary>
    [HttpGet("trimestral/cicle/{cicleId:guid}")]
    public async Task<ActionResult<ApiResponse<InformeTrimestralDto>>> GetInformeTrimestralCicle(
        Guid cicleId, [FromQuery] int trimestre, [FromQuery] Guid anyAcademicId, CancellationToken ct)
    {
        if (trimestre < 1 || trimestre > 3)
            return BadRequest(ApiResponse<InformeTrimestralDto>.Fail("El trimestre ha de ser 1, 2 o 3"));

        var informe = await _informesService.GenerarInformeTrimestralCicleAsync(
            cicleId, trimestre, anyAcademicId, ct);
        return Ok(ApiResponse<InformeTrimestralDto>.Ok(informe));
    }

    /// <summary>
    /// Envia l'informe mensual per correu a la direcció de l'escola i al tutor del grup.
    /// </summary>
    [HttpPost("enviar-mensual")]
    [Authorize(Policy = "NomesEquipDirectiu")]
    public async Task<ActionResult<ApiResponse>> EnviarInformeMensual(
        [FromBody] EnviarInformeDto dto, CancellationToken ct)
    {
        var informe = await _informesService.GenerarInformeMensualGrupAsync(dto.GrupId, dto.Any, dto.Mes, ct);

        var nomMes = new System.Globalization.DateTimeFormatInfo { MonthNames = ["Gener","Febrer","Març","Abril","Maig","Juny","Juliol","Agost","Setembre","Octubre","Novembre","Desembre",""] }.GetMonthName(dto.Mes);
        var assumpte = $"Informe d'assistència {nomMes} {dto.Any} - {informe.GrupNom}";

        var cosHtml = GenerarHtmlInformeMensual(informe);

        // Enviar a l'escola
        var emailEscola = _config["Escola:EmailInformes"] ?? "a8059721@xtec.cat";
        await _emailService.SendGenericEmailAsync(emailEscola, assumpte, cosHtml, ct);

        _logger.LogInformation(
            "Informe mensual enviat: Grup {GrupNom}, {Mes}/{Any}",
            informe.GrupNom, dto.Mes, dto.Any);

        return Ok(ApiResponse.Ok());
    }

    // ── Exportació Excel ─────────────────────────────────────

    /// <summary>Descarrega l'informe mensual en format Excel.</summary>
    [HttpGet("export-excel/mensual/grup/{grupId:guid}")]
    public async Task<IActionResult> ExportExcelMensualGrup(
        Guid grupId, [FromQuery] int any, [FromQuery] int mes, CancellationToken ct)
    {
        if (mes < 1 || mes > 12)
            return BadRequest("El mes ha de ser entre 1 i 12");

        var informe = await _informesService.GenerarInformeMensualGrupAsync(grupId, any, mes, ct);
        var bytes = _excelService.ExportarInformeMensual(informe);

        var nomMes = new System.Globalization.CultureInfo("ca-ES")
            .DateTimeFormat.GetMonthName(mes);
        var nomFitxer = $"assistencia_{informe.GrupNom?.Replace(" ", "_")}_{nomMes}_{any}.xlsx";

        return File(bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            nomFitxer);
    }

    /// <summary>Descarrega l'informe trimestral en format Excel.</summary>
    [HttpGet("export-excel/trimestral/grup/{grupId:guid}")]
    public async Task<IActionResult> ExportExcelTrimestralGrup(
        Guid grupId, [FromQuery] int trimestre, [FromQuery] Guid anyAcademicId, CancellationToken ct)
    {
        if (trimestre < 1 || trimestre > 3)
            return BadRequest("El trimestre ha de ser 1, 2 o 3");

        var informe = await _informesService.GenerarInformeTrimestralGrupAsync(
            grupId, trimestre, anyAcademicId, ct);
        var bytes = _excelService.ExportarInformeTrimestral(informe);

        var nomFitxer = $"assistencia_{informe.GrupNom?.Replace(" ", "_")}_T{trimestre}.xlsx";

        return File(bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            nomFitxer);
    }

    // ── Helpers ─────────────────────────────────────────────

    private static string GenerarHtmlInformeMensual(InformeMensualDto informe)
    {
        var alumnesAlerta = informe.Alumnes.Where(a => a.EsAlerta && !a.EsCritic).ToList();
        var alumnesCritic = informe.Alumnes.Where(a => a.EsCritic).ToList();

        static string H(string? s) => WebUtility.HtmlEncode(s ?? string.Empty);

        return $"""
            <h2>Informe d'assistència mensual - {H(informe.GrupNom ?? informe.CicleNom)}</h2>
            <p><strong>Any acadèmic:</strong> {H(informe.AnyAcademic)}</p>
            <p><strong>Període:</strong> {informe.Mes}/{informe.Any}</p>
            <p><strong>Total alumnes:</strong> {informe.TotalAlumnes}</p>

            <h3>Alumnes en situació crítica (&gt;25% absència)</h3>
            {(alumnesCritic.Any()
                ? $"<ul>{string.Join("", alumnesCritic.Select(a => $"<li><strong>{H(a.AlumneNom)}</strong> — {a.PercentatgeAbsencia}%</li>"))}</ul>"
                : "<p>Cap alumne en situació crítica.</p>")}

            <h3>Alumnes en alerta (&gt;10% absència)</h3>
            {(alumnesAlerta.Any()
                ? $"<ul>{string.Join("", alumnesAlerta.Select(a => $"<li>{H(a.AlumneNom)} — {a.PercentatgeAbsencia}%</li>"))}</ul>"
                : "<p>Cap alumne en alerta.</p>")}

            <p><em>Generat automàticament per AssistenciaPlus</em></p>
            """;
    }
}
