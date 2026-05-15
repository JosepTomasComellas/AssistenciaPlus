using AssistenciaPlus.Application.Common;
using AssistenciaPlus.Application.DTOs;
using AssistenciaPlus.Application.Interfaces;
using AssistenciaPlus.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AssistenciaPlus.Api.Controllers;

[ApiController]
[Route("api/grups")]
[Authorize]
public class GrupsController : BaseApiController
{
    private readonly IGrupRepository _grupRepo;
    private readonly ICalendariRepository _calendariRepo;

    public GrupsController(IGrupRepository grupRepo, ICalendariRepository calendariRepo)
    {
        _grupRepo = grupRepo;
        _calendariRepo = calendariRepo;
    }

    /// <summary>
    /// Llista els grups del mestre autenticat per a l'any acadèmic actiu.
    /// L'equip directiu i els administratius veuen tots els grups.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<GrupDto>>>> GetGrups(CancellationToken ct)
    {
        var anyActiu = await _calendariRepo.GetAnyAcademicActiuAsync(ct);
        if (anyActiu == null)
            return Ok(ApiResponse<IEnumerable<GrupDto>>.Ok([]));

        var rol = User.FindFirst(ClaimTypes.Role)?.Value;
        IReadOnlyList<Grup> grups;

        if (rol is nameof(RolUsuari.EquipDirectiu) or nameof(RolUsuari.Administratiu))
        {
            grups = await _grupRepo.GetPerAnyAcademicAsync(anyActiu.Id, ct);
        }
        else
        {
            var mestreId = GetCurrentUserId();
            grups = await _grupRepo.GetPerMestreAsync(mestreId, anyActiu.Id, ct);
        }

        return Ok(ApiResponse<IEnumerable<GrupDto>>.Ok(grups.Select(MaparGrup)));
    }

    /// <summary>Detall d'un grup concret.</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<GrupDto>>> GetGrup(Guid id, CancellationToken ct)
    {
        var grup = await _grupRepo.GetByIdAmbDetallsAsync(id, ct);
        if (grup == null) return NotFound(ApiResponse<GrupDto>.Fail("Grup no trobat"));

        return Ok(ApiResponse<GrupDto>.Ok(MaparGrup(grup)));
    }

    /// <summary>Llista les franges horàries disponibles.</summary>
    [HttpGet("franjes")]
    public async Task<ActionResult<ApiResponse<IEnumerable<FranjaHorariaDto>>>> GetFranjes(CancellationToken ct)
    {
        var franjes = await _calendariRepo.GetFranjesAsync(ct);
        return Ok(ApiResponse<IEnumerable<FranjaHorariaDto>>.Ok(franjes.Select(f => new FranjaHorariaDto
        {
            Id = f.Id,
            Nom = f.Nom,
            HoraInici = f.HoraInici,
            HoraFi = f.HoraFi,
            EsMati = f.EsMati,
            EsJornadaIntensiva = f.EsJornadaIntensiva,
            Ordre = f.Ordre
        })));
    }

    /// <summary>Llista tots els cicles i cursos.</summary>
    [HttpGet("cicles")]
    public async Task<ActionResult<ApiResponse<IEnumerable<CicleDto>>>> GetCicles(CancellationToken ct)
    {
        var cicles = await _grupRepo.GetCiclesAsync(ct);
        return Ok(ApiResponse<IEnumerable<CicleDto>>.Ok(cicles.Select(c => new CicleDto
        {
            Id = c.Id,
            Nom = c.Nom,
            Ordre = c.Ordre,
            Cursos = c.Cursos.OrderBy(cu => cu.Ordre).Select(cu => new CursDto
            {
                Id = cu.Id,
                CicleId = cu.CicleId,
                Nom = cu.Nom,
                Codi = cu.Codi,
                Ordre = cu.Ordre,
                UsaModeFusteta = cu.UsaModeFusteta
            }).ToList()
        })));
    }

    // ── Helpers ─────────────────────────────────────────────

    private static GrupDto MaparGrup(Grup g) => new()
    {
        Id = g.Id,
        Lletra = g.Lletra,
        NomComplet = g.NomComplet,
        CursId = g.CursId,
        CursNom = g.Curs?.Nom ?? string.Empty,
        CursCodi = g.Curs?.Codi ?? string.Empty,
        UsaModeFusteta = g.Curs?.UsaModeFusteta ?? false,
        CicleId = g.Curs?.CicleId ?? Guid.Empty,
        CicleNom = g.Curs?.Cicle?.Nom ?? string.Empty,
        AnyAcademicId = g.AnyAcademicId,
        TutorId = g.TutorId,
        TutorNomComplet = g.Tutor?.NomComplet,
        NombreAlumnes = g.Alumnes?.Count(a => a.EsActiu) ?? 0
    };
}
