using AssistenciaPlus.Application.Common;
using AssistenciaPlus.Application.Interfaces;
using AssistenciaPlus.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace AssistenciaPlus.Api.Controllers;

[ApiController]
[Route("api/ia")]
[Authorize(Roles = "EquipDirectiu")]
public class IaController : BaseApiController
{
    private readonly IOllamaService _ollama;
    private readonly IAssistenciaRepository _assistenciaRepo;
    private readonly ILogger<IaController> _logger;

    public IaController(
        IOllamaService ollama,
        IAssistenciaRepository assistenciaRepo,
        ILogger<IaController> logger)
    {
        _ollama = ollama;
        _assistenciaRepo = assistenciaRepo;
        _logger = logger;
    }

    [HttpGet("disponible")]
    public async Task<ActionResult<ApiResponse<bool>>> GetDisponible(CancellationToken ct)
    {
        var disponible = await _ollama.IsAvailableAsync(ct);
        return Ok(ApiResponse<bool>.Ok(disponible));
    }

    [HttpPost("consulta")]
    public async Task<ActionResult<ApiResponse<ConsultaIaResultat>>> PostConsulta(
        [FromBody] ConsultaIaDto dto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.Pregunta))
            return BadRequest(ApiResponse<ConsultaIaResultat>.Fail("La pregunta no pot estar buida"));

        if (!await _ollama.IsAvailableAsync(ct))
            return Ok(ApiResponse<ConsultaIaResultat>.Ok(new ConsultaIaResultat
            {
                Resposta = "El servei d'IA (Ollama) no està disponible en aquest moment. Comprova que el contenidor Ollama s'està executant.",
                EstaDisponible = false
            }));

        var avui = DateOnly.FromDateTime(DateTime.Today);
        var context = await ConstruirContextAsync(dto.AnyAcademicId, avui, ct);

        _logger.LogInformation("Consulta IA: {Pregunta}", dto.Pregunta);
        var resposta = await _ollama.QueryAsync(dto.Pregunta, context, ct);

        return Ok(ApiResponse<ConsultaIaResultat>.Ok(new ConsultaIaResultat
        {
            Resposta = resposta,
            EstaDisponible = true
        }));
    }

    private async Task<string> ConstruirContextAsync(Guid anyAcademicId, DateOnly avui, CancellationToken ct)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Data actual: {avui:dd/MM/yyyy} ({avui.DayOfWeek switch {
            DayOfWeek.Monday => "dilluns", DayOfWeek.Tuesday => "dimarts",
            DayOfWeek.Wednesday => "dimecres", DayOfWeek.Thursday => "dijous",
            DayOfWeek.Friday => "divendres", DayOfWeek.Saturday => "dissabte",
            _ => "diumenge" }})");
        sb.AppendLine();

        // KPIs d'avui
        try
        {
            var kpis = await _assistenciaRepo.GetKpisDashboardAsync(anyAcademicId, avui, ct);
            sb.AppendLine("RESUM D'AVUI:");
            sb.AppendLine($"- Sessions registrades avui: {kpis.SessionsAvui}");
            sb.AppendLine($"- Grups amb llista passada: {kpis.GrupsAmbLlista} de {kpis.GrupsTotals}");
            sb.AppendLine($"- Absències totals avui: {kpis.AbsenciesAvui}");
            if (kpis.GrupsSenseLlista.Count > 0)
                sb.AppendLine($"- Grups que encara no han passat llista: {string.Join(", ", kpis.GrupsSenseLlista)}");
            else
                sb.AppendLine("- Tots els grups han passat llista avui.");
            sb.AppendLine();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error obtenint KPIs per context IA");
        }

        // Top absents de l'any
        try
        {
            var resum = await _assistenciaRepo.GetResumAlumnesAsync(anyAcademicId, ct: ct);
            var top10 = resum.Take(10).ToList();
            if (top10.Count > 0)
            {
                sb.AppendLine("TOP 10 ALUMNES AMB MÉS ABSÈNCIES (any complet):");
                for (int i = 0; i < top10.Count; i++)
                {
                    var a = top10[i];
                    var pct = a.TotalSessions > 0
                        ? (double)a.TotalAbsents / a.TotalSessions * 100
                        : 0;
                    sb.AppendLine($"{i + 1}. {a.AlumneNom} ({a.GrupNom}): {a.TotalAbsents} absències de {a.TotalSessions} sessions ({pct:F1}%)");
                }
                sb.AppendLine();

                var totalAlumnes = resum.Count;
                var totalSessions = resum.Sum(r => r.TotalSessions);
                var totalAbsencies = resum.Sum(r => r.TotalAbsents);
                sb.AppendLine("ESTADÍSTICA GENERAL DE L'ANY:");
                sb.AppendLine($"- Alumnes amb registre: {totalAlumnes}");
                sb.AppendLine($"- Total entrades d'assistència: {totalSessions}");
                sb.AppendLine($"- Total absències acumulades: {totalAbsencies}");
                if (totalSessions > 0)
                    sb.AppendLine($"- Percentatge d'absència global: {(double)totalAbsencies / totalSessions * 100:F1}%");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error obtenint resum alumnes per context IA");
        }

        return sb.ToString();
    }
}

public record ConsultaIaDto
{
    public string Pregunta { get; init; } = string.Empty;
    public Guid AnyAcademicId { get; init; }
}

public record ConsultaIaResultat
{
    public string Resposta { get; init; } = string.Empty;
    public bool EstaDisponible { get; init; }
}
