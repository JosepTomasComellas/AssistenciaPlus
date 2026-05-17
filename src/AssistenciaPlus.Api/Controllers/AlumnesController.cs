using AssistenciaPlus.Api.Helpers;
using AssistenciaPlus.Application.Common;
using AssistenciaPlus.Application.DTOs;
using AssistenciaPlus.Application.Interfaces;
using AssistenciaPlus.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AssistenciaPlus.Api.Controllers;

[ApiController]
[Route("api/alumnes")]
[Authorize]
public class AlumnesController : BaseApiController
{
    private readonly IAlumneRepository _alumneRepo;
    private readonly IGrupRepository _grupRepo;
    private readonly IWebHostEnvironment _env;

    private static readonly string[] _mimesAcceptats = ["image/jpeg", "image/png", "image/webp"];
    private const long MidaMaxima = 2 * 1024 * 1024; // 2 MB

    public AlumnesController(IAlumneRepository alumneRepo, IGrupRepository grupRepo, IWebHostEnvironment env)
    {
        _alumneRepo = alumneRepo;
        _grupRepo = grupRepo;
        _env = env;
    }

    /// <summary>Llista els alumnes d'un grup.</summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<AlumneDto>>>> GetAlumnes(
        [FromQuery] Guid grupId, CancellationToken ct)
    {
        if (grupId == Guid.Empty)
            return BadRequest(ApiResponse<IEnumerable<AlumneDto>>.Fail("Cal indicar el grupId"));

        var rol = User.FindFirst(ClaimTypes.Role)?.Value;
        if (rol == nameof(RolUsuari.Mestre))
        {
            var mestreId = GetCurrentUserId();
            var grup = await _grupRepo.GetByIdAsync(grupId, ct);
            if (grup?.TutorId != mestreId)
                return Forbid();
        }

        var alumnes = await _alumneRepo.GetPerGrupAsync(grupId, ct);
        return Ok(ApiResponse<IEnumerable<AlumneDto>>.Ok(alumnes.Select(MaparAlumne)));
    }

    /// <summary>Detall d'un alumne.</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<AlumneDto>>> GetAlumne(Guid id, CancellationToken ct)
    {
        var alumne = await _alumneRepo.GetByIdAsync(id, ct);
        if (alumne == null) return NotFound(ApiResponse<AlumneDto>.Fail("Alumne no trobat"));
        return Ok(ApiResponse<AlumneDto>.Ok(MaparAlumne(alumne)));
    }

    /// <summary>Crea un alumne nou. Requereix rol Equip Directiu.</summary>
    [HttpPost]
    [Authorize(Policy = "NomesEquipDirectiu")]
    public async Task<ActionResult<ApiResponse<AlumneDto>>> CrearAlumne(
        [FromBody] CrearAlumneDto dto, CancellationToken ct)
    {
        var grup = await _grupRepo.GetByIdAsync(dto.GrupId, ct);
        if (grup == null)
            return NotFound(ApiResponse<AlumneDto>.Fail("Grup no trobat"));

        var alumne = new Alumne
        {
            Nom = dto.Nom,
            Cognom1 = dto.Cognom1,
            Cognom2 = dto.Cognom2,
            DataNaixement = dto.DataNaixement,
            GrupId = dto.GrupId,
            OrdreFusteta = dto.OrdreFusteta,
            EsActiu = true
        };

        await _alumneRepo.AfegirAsync(alumne, ct);
        await _alumneRepo.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetAlumne), new { id = alumne.Id },
            ApiResponse<AlumneDto>.Ok(MaparAlumne(alumne)));
    }

    /// <summary>Actualitza un alumne. Requereix rol Equip Directiu.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = "NomesEquipDirectiu")]
    public async Task<ActionResult<ApiResponse>> ActualitzarAlumne(
        Guid id, [FromBody] ActualitzarAlumneDto dto, CancellationToken ct)
    {
        var alumne = await _alumneRepo.GetByIdAsync(id, ct);
        if (alumne == null) return NotFound(ApiResponse.Fail("Alumne no trobat"));

        alumne.Nom = dto.Nom;
        alumne.Cognom1 = dto.Cognom1;
        alumne.Cognom2 = dto.Cognom2;
        alumne.DataNaixement = dto.DataNaixement;
        alumne.OrdreFusteta = dto.OrdreFusteta;
        alumne.EsActiu = dto.EsActiu;

        await _alumneRepo.ActualitzarAsync(alumne, ct);
        await _alumneRepo.SaveChangesAsync(ct);

        return Ok(ApiResponse.Ok());
    }

    /// <summary>Elimina (soft delete) un alumne. Requereix rol Equip Directiu.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "NomesEquipDirectiu")]
    public async Task<ActionResult<ApiResponse>> EsborrarAlumne(Guid id, CancellationToken ct)
    {
        var alumne = await _alumneRepo.GetByIdAsync(id, ct);
        if (alumne == null) return NotFound(ApiResponse.Fail("Alumne no trobat"));

        await _alumneRepo.EsborrarAsync(id, ct);
        await _alumneRepo.SaveChangesAsync(ct);

        return Ok(ApiResponse.Ok());
    }

    /// <summary>Puja o substitueix la foto d'un alumne. Requereix rol Equip Directiu.</summary>
    [HttpPut("{id:guid}/foto")]
    [Authorize(Policy = "NomesEquipDirectiu")]
    [RequestSizeLimit(2 * 1024 * 1024 + 4096)]
    public async Task<ActionResult<ApiResponse<string>>> PujarFotoAlumne(
        Guid id, IFormFile foto, CancellationToken ct)
    {
        if (foto is null || foto.Length == 0)
            return BadRequest(ApiResponse<string>.Fail("Cal seleccionar una imatge"));

        if (foto.Length > MidaMaxima)
            return BadRequest(ApiResponse<string>.Fail("La imatge no pot superar els 2 MB"));

        if (!_mimesAcceptats.Contains(foto.ContentType.ToLower()))
            return BadRequest(ApiResponse<string>.Fail("Format no acceptat. Usa JPEG, PNG o WebP"));

        var alumne = await _alumneRepo.GetByIdAsync(id, ct);
        if (alumne == null) return NotFound(ApiResponse<string>.Fail("Alumne no trobat"));

        var dirFotos = Path.Combine(_env.ContentRootPath, "uploads", "alumnes");
        Directory.CreateDirectory(dirFotos);

        foreach (var antic in Directory.GetFiles(dirFotos, $"{id}.*"))
        {
            try { System.IO.File.Delete(antic); } catch (IOException) { }
        }

        var nomFitxer = $"{id}.jpg";
        using var stream = foto.OpenReadStream();
        await FotoHelper.ResitzarIGuardarAsync(stream, Path.Combine(dirFotos, nomFitxer), ct);

        var versio = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        alumne.FotoPath = $"/uploads/alumnes/{nomFitxer}?v={versio}";
        await _alumneRepo.ActualitzarAsync(alumne, ct);
        await _alumneRepo.SaveChangesAsync(ct);

        return Ok(ApiResponse<string>.Ok(alumne.FotoPath));
    }

    // ── Helpers ─────────────────────────────────────────────

    private static AlumneDto MaparAlumne(Alumne a) => new()
    {
        Id = a.Id,
        Nom = a.Nom,
        Cognom1 = a.Cognom1,
        Cognom2 = a.Cognom2,
        NomComplet = a.NomComplet,
        NomFusteta = a.NomFusteta,
        DataNaixement = a.DataNaixement,
        FotoPath = a.FotoPath,
        GrupId = a.GrupId,
        OrdreFusteta = a.OrdreFusteta,
        EsActiu = a.EsActiu
    };
}
