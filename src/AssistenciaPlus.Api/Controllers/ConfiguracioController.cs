using AssistenciaPlus.Application.Common;
using AssistenciaPlus.Application.DTOs;
using AssistenciaPlus.Application.Interfaces;
using AssistenciaPlus.Core.Interfaces;
using AssistenciaPlus.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssistenciaPlus.Api.Controllers;

/// <summary>
/// Configuració del sistema: calendari escolar i gestió d'usuaris.
/// Accés restringit a l'Equip Directiu.
/// </summary>
[ApiController]
[Route("api/configuracio")]
[Authorize(Policy = "NomesEquipDirectiu")]
public class ConfiguracioController : ControllerBase
{
    private readonly ICalendariRepository _calendariRepo;
    private readonly IUsuariRepository _usuariRepo;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _config;
    private readonly ILogger<ConfiguracioController> _logger;

    public ConfiguracioController(
        ICalendariRepository calendariRepo,
        IUsuariRepository usuariRepo,
        IEmailService emailService,
        IConfiguration config,
        ILogger<ConfiguracioController> logger)
    {
        _calendariRepo = calendariRepo;
        _usuariRepo = usuariRepo;
        _emailService = emailService;
        _config = config;
        _logger = logger;
    }

    // ── Calendari ────────────────────────────────────────────

    /// <summary>Llista tots els anys acadèmics.</summary>
    [HttpGet("anys-academics")]
    public async Task<ActionResult<ApiResponse<IEnumerable<AnyAcademicDto>>>> GetAnysAcademics(CancellationToken ct)
    {
        var anys = await _calendariRepo.GetAnysAcademicsAsync(ct);
        return Ok(ApiResponse<IEnumerable<AnyAcademicDto>>.Ok(anys.Select(MaparAnyAcademic)));
    }

    /// <summary>Retorna els dies del calendari d'un any acadèmic.</summary>
    [HttpGet("calendari/{anyAcademicId:guid}")]
    public async Task<ActionResult<ApiResponse<IEnumerable<DiaCalendariDto>>>> GetCalendari(
        Guid anyAcademicId, [FromQuery] DateOnly? dataInici, [FromQuery] DateOnly? dataFi, CancellationToken ct)
    {
        var anyAcademic = await _calendariRepo.GetAnyAcademicPerIdAsync(anyAcademicId, ct);
        if (anyAcademic == null)
            return NotFound(ApiResponse<IEnumerable<DiaCalendariDto>>.Fail("Any acadèmic no trobat"));

        var inici = dataInici ?? anyAcademic.DataInici;
        var fi = dataFi ?? anyAcademic.DataFi;

        var dies = await _calendariRepo.GetDiesRangAsync(anyAcademicId, inici, fi, ct);
        return Ok(ApiResponse<IEnumerable<DiaCalendariDto>>.Ok(dies.Select(d => new DiaCalendariDto
        {
            Id = d.Id,
            AnyAcademicId = d.AnyAcademicId,
            Data = d.Data,
            TipusDia = d.TipusDia,
            Descripcio = d.Descripcio
        })));
    }

    /// <summary>Actualitza o crea el tipus d'un dia del calendari.</summary>
    [HttpPut("calendari/dia")]
    public async Task<ActionResult<ApiResponse>> ActualitzarDia(
        [FromBody] ActualitzarDiaCalendariDto dto, CancellationToken ct)
    {
        var diaExistent = await _calendariRepo.GetDiaAsync(dto.AnyAcademicId, dto.Data, ct);

        if (diaExistent != null)
        {
            diaExistent.TipusDia = dto.TipusDia;
            diaExistent.Descripcio = dto.Descripcio;
            await _calendariRepo.ActualitzarDiaAsync(diaExistent, ct);
        }
        else
        {
            await _calendariRepo.AfegirDiesAsync([new DiaCalendari
            {
                AnyAcademicId = dto.AnyAcademicId,
                Data = dto.Data,
                TipusDia = dto.TipusDia,
                Descripcio = dto.Descripcio
            }], ct);
        }

        await _calendariRepo.SaveChangesAsync(ct);
        return Ok(ApiResponse.Ok());
    }

    /// <summary>Configura múltiples dies del calendari d'una vegada.</summary>
    [HttpPost("calendari/dies")]
    public async Task<ActionResult<ApiResponse>> ConfigurarDies(
        [FromBody] ConfigurarDiesDto dto, CancellationToken ct)
    {
        foreach (var diaDto in dto.Dies)
        {
            var diaExistent = await _calendariRepo.GetDiaAsync(dto.AnyAcademicId, diaDto.Data, ct);

            if (diaExistent != null)
            {
                diaExistent.TipusDia = diaDto.TipusDia;
                diaExistent.Descripcio = diaDto.Descripcio;
                await _calendariRepo.ActualitzarDiaAsync(diaExistent, ct);
            }
            else
            {
                await _calendariRepo.AfegirDiesAsync([new DiaCalendari
                {
                    AnyAcademicId = dto.AnyAcademicId,
                    Data = diaDto.Data,
                    TipusDia = diaDto.TipusDia,
                    Descripcio = diaDto.Descripcio
                }], ct);
            }
        }

        await _calendariRepo.SaveChangesAsync(ct);
        _logger.LogInformation("Calendari configurat: {Count} dies actualitzats", dto.Dies.Count);
        return Ok(ApiResponse.Ok());
    }

    // ── Usuaris ──────────────────────────────────────────────

    /// <summary>Llista tots els usuaris del sistema.</summary>
    [HttpGet("usuaris")]
    public async Task<ActionResult<ApiResponse<IEnumerable<UsuariDto>>>> GetUsuaris(CancellationToken ct)
    {
        var usuaris = await _usuariRepo.GetTotsAsync(ct);
        return Ok(ApiResponse<IEnumerable<UsuariDto>>.Ok(usuaris.Select(MaparUsuari)));
    }

    /// <summary>Crea un usuari nou i envia les credencials per correu.</summary>
    [HttpPost("usuaris")]
    public async Task<ActionResult<ApiResponse<UsuariDto>>> CrearUsuari(
        [FromBody] CrearUsuariDto dto, CancellationToken ct)
    {
        if (!Enum.TryParse<RolUsuari>(dto.Rol, out var rol))
            return BadRequest(ApiResponse<UsuariDto>.Fail($"Rol invàlid: {dto.Rol}"));

        var contrasenyaTemporal = GenerarContrasenyaTemporal();
        var usuari = new Usuari
        {
            Nom = dto.Nom,
            Cognom1 = dto.Cognom1,
            Cognom2 = dto.Cognom2,
            Email = dto.Email.ToLowerInvariant(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(contrasenyaTemporal, workFactor: 12),
            Rol = rol,
            Idioma = dto.Idioma,
            EsActiu = true
        };

        await _usuariRepo.AfegirAsync(usuari, ct);
        await _usuariRepo.SaveChangesAsync(ct);

        var urlApp = _config["App:PublicUrl"] ?? "https://localhost";
        await _emailService.SendWelcomeEmailAsync(
            usuari.Email, usuari.NomComplet, contrasenyaTemporal, urlApp, ct);

        _logger.LogInformation("Usuari creat: {Email} ({Rol})", usuari.Email, usuari.Rol);

        return CreatedAtAction(nameof(GetUsuaris), ApiResponse<UsuariDto>.Ok(MaparUsuari(usuari)));
    }

    /// <summary>Actualitza un usuari existent.</summary>
    [HttpPut("usuaris/{id:guid}")]
    public async Task<ActionResult<ApiResponse>> ActualitzarUsuari(
        Guid id, [FromBody] ActualitzarUsuariDto dto, CancellationToken ct)
    {
        var usuari = await _usuariRepo.GetByIdAsync(id, ct);
        if (usuari == null) return NotFound(ApiResponse.Fail("Usuari no trobat"));

        if (!Enum.TryParse<RolUsuari>(dto.Rol, out var rol))
            return BadRequest(ApiResponse.Fail($"Rol invàlid: {dto.Rol}"));

        usuari.Nom = dto.Nom;
        usuari.Cognom1 = dto.Cognom1;
        usuari.Cognom2 = dto.Cognom2;
        usuari.Email = dto.Email.ToLowerInvariant();
        usuari.Rol = rol;
        usuari.Idioma = dto.Idioma;
        usuari.EsActiu = dto.EsActiu;

        await _usuariRepo.ActualitzarAsync(usuari, ct);
        await _usuariRepo.SaveChangesAsync(ct);

        return Ok(ApiResponse.Ok());
    }

    /// <summary>Elimina (soft delete) un usuari.</summary>
    [HttpDelete("usuaris/{id:guid}")]
    public async Task<ActionResult<ApiResponse>> EsborrarUsuari(Guid id, CancellationToken ct)
    {
        var usuari = await _usuariRepo.GetByIdAsync(id, ct);
        if (usuari == null) return NotFound(ApiResponse.Fail("Usuari no trobat"));

        await _usuariRepo.EsborrarAsync(id, ct);
        await _usuariRepo.SaveChangesAsync(ct);

        return Ok(ApiResponse.Ok());
    }

    /// <summary>Reenvia les credencials d'accés a un usuari per correu.</summary>
    [HttpPost("usuaris/{id:guid}/enviar-credencials")]
    public async Task<ActionResult<ApiResponse>> EnviarCredencials(Guid id, CancellationToken ct)
    {
        var usuari = await _usuariRepo.GetByIdAsync(id, ct);
        if (usuari == null) return NotFound(ApiResponse.Fail("Usuari no trobat"));

        var novaContrasenya = GenerarContrasenyaTemporal();
        usuari.PasswordHash = BCrypt.Net.BCrypt.HashPassword(novaContrasenya, workFactor: 12);
        await _usuariRepo.ActualitzarAsync(usuari, ct);
        await _usuariRepo.SaveChangesAsync(ct);

        var urlApp = _config["App:PublicUrl"] ?? "https://localhost";
        await _emailService.SendWelcomeEmailAsync(
            usuari.Email, usuari.NomComplet, novaContrasenya, urlApp, ct);

        return Ok(ApiResponse.Ok());
    }

    // ── Helpers ─────────────────────────────────────────────

    private static AnyAcademicDto MaparAnyAcademic(AnyAcademic a) => new()
    {
        Id = a.Id,
        Nom = a.Nom,
        DataInici = a.DataInici,
        DataFi = a.DataFi,
        EsActiu = a.EsActiu,
        IniciFT1 = a.IniciFT1,
        FiT1 = a.FiT1,
        IniciFT2 = a.IniciFT2,
        FiT2 = a.FiT2,
        IniciFT3 = a.IniciFT3,
        FiT3 = a.FiT3
    };

    private static UsuariDto MaparUsuari(Usuari u) => new()
    {
        Id = u.Id,
        Nom = u.Nom,
        Cognom1 = u.Cognom1,
        Cognom2 = u.Cognom2,
        NomComplet = u.NomComplet,
        Email = u.Email,
        Rol = u.Rol.ToString(),
        FotoPath = u.FotoPath,
        EsActiu = u.EsActiu,
        Idioma = u.Idioma,
        UltimAcces = u.UltimAcces
    };

    private static string GenerarContrasenyaTemporal()
    {
        const string chars = "ABCDEFGHJKMNPQRSTUVWXYZabcdefghjkmnpqrstuvwxyz23456789!@#";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, 12)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }
}
