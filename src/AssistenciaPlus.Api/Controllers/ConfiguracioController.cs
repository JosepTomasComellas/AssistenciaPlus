using AssistenciaPlus.Api.Helpers;
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
    private readonly IGrupRepository _grupRepo;
    private readonly IUsuariRepository _usuariRepo;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _config;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<ConfiguracioController> _logger;

    private static readonly string[] _mimesAcceptats = ["image/jpeg", "image/png", "image/webp"];
    private const long MidaMaxima = 2 * 1024 * 1024;

    public ConfiguracioController(
        ICalendariRepository calendariRepo,
        IGrupRepository grupRepo,
        IUsuariRepository usuariRepo,
        IEmailService emailService,
        IConfiguration config,
        IWebHostEnvironment env,
        ILogger<ConfiguracioController> logger)
    {
        _calendariRepo = calendariRepo;
        _grupRepo = grupRepo;
        _usuariRepo = usuariRepo;
        _emailService = emailService;
        _config = config;
        _env = env;
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

        var emailNormalitzat = dto.Email.ToLowerInvariant();
        var existent = await _usuariRepo.GetPerEmailAsync(emailNormalitzat, ct);
        if (existent != null)
            return Conflict(ApiResponse<UsuariDto>.Fail("Ja existeix un usuari amb aquest correu electrònic"));

        var contrasenyaTemporal = GenerarContrasenyaTemporal();
        var usuari = new Usuari
        {
            Nom = dto.Nom,
            Cognom1 = dto.Cognom1,
            Cognom2 = dto.Cognom2,
            Email = emailNormalitzat,
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

        _logger.LogInformation("Usuari creat amb rol {Rol}", usuari.Rol);

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

    /// <summary>Puja o substitueix la foto de perfil d'un usuari.</summary>
    [HttpPut("usuaris/{id:guid}/foto")]
    [RequestSizeLimit(2 * 1024 * 1024 + 4096)]
    public async Task<ActionResult<ApiResponse<string>>> PujarFotoUsuari(
        Guid id, IFormFile foto, CancellationToken ct)
    {
        if (foto is null || foto.Length == 0)
            return BadRequest(ApiResponse<string>.Fail("Cal seleccionar una imatge"));

        if (foto.Length > MidaMaxima)
            return BadRequest(ApiResponse<string>.Fail("La imatge no pot superar els 2 MB"));

        if (!_mimesAcceptats.Contains(foto.ContentType.ToLower()))
            return BadRequest(ApiResponse<string>.Fail("Format no acceptat. Usa JPEG, PNG o WebP"));

        var usuari = await _usuariRepo.GetByIdAsync(id, ct);
        if (usuari == null) return NotFound(ApiResponse<string>.Fail("Usuari no trobat"));

        var dirFotos = Path.Combine(_env.ContentRootPath, "uploads", "usuaris");
        Directory.CreateDirectory(dirFotos);

        foreach (var antic in Directory.GetFiles(dirFotos, $"{id}.*"))
            System.IO.File.Delete(antic);

        var nomFitxer = $"{id}.jpg";
        using var stream = foto.OpenReadStream();
        await FotoHelper.ResitzarIGuardarAsync(stream, Path.Combine(dirFotos, nomFitxer), ct);

        var versio = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        usuari.FotoPath = $"/uploads/usuaris/{nomFitxer}?v={versio}";
        await _usuariRepo.ActualitzarAsync(usuari, ct);
        await _usuariRepo.SaveChangesAsync(ct);

        return Ok(ApiResponse<string>.Ok(usuari.FotoPath));
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

    // ── Anys acadèmics (crear / activar) ─────────────────────

    /// <summary>Crea un any acadèmic nou (inactiu per defecte).</summary>
    [HttpPost("anys-academics")]
    public async Task<ActionResult<ApiResponse<AnyAcademicDto>>> CrearAnyAcademic(
        [FromBody] CrearAnyAcademicDto dto, CancellationToken ct)
    {
        var any = new AnyAcademic
        {
            Nom = dto.Nom,
            DataInici = dto.DataInici,
            DataFi = dto.DataFi,
            EsActiu = false,
            IniciT1 = dto.IniciT1,
            FiT1 = dto.FiT1,
            IniciT2 = dto.IniciT2,
            FiT2 = dto.FiT2,
            IniciT3 = dto.IniciT3,
            FiT3 = dto.FiT3,
        };

        await _calendariRepo.AfegirAnyAcademicAsync(any, ct);
        await _calendariRepo.SaveChangesAsync(ct);

        _logger.LogInformation("Any acadèmic creat: {Nom}", any.Nom);
        return CreatedAtAction(nameof(GetAnysAcademics), ApiResponse<AnyAcademicDto>.Ok(MaparAnyAcademic(any)));
    }

    /// <summary>Activa un any acadèmic i desactiva la resta.</summary>
    [HttpPost("anys-academics/{id:guid}/activar")]
    public async Task<ActionResult<ApiResponse>> ActivarAnyAcademic(Guid id, CancellationToken ct)
    {
        var any = await _calendariRepo.GetAnyAcademicPerIdAsync(id, ct);
        if (any == null) return NotFound(ApiResponse.Fail("Any acadèmic no trobat"));

        var tots = await _calendariRepo.GetAnysAcademicsAsync(ct);
        foreach (var a in tots.Where(a => a.EsActiu))
        {
            a.EsActiu = false;
            await _calendariRepo.ActualitzarAnyAcademicAsync(a, ct);
        }

        any.EsActiu = true;
        await _calendariRepo.ActualitzarAnyAcademicAsync(any, ct);
        await _calendariRepo.SaveChangesAsync(ct);

        _logger.LogInformation("Any acadèmic activat: {Nom}", any.Nom);
        return Ok(ApiResponse.Ok());
    }

    // ── Cicles i cursos ──────────────────────────────────────

    /// <summary>Llista tots els cicles amb els seus cursos.</summary>
    [HttpGet("cicles")]
    public async Task<ActionResult<ApiResponse<IEnumerable<CicleDto>>>> GetCicles(CancellationToken ct)
    {
        var cicles = await _grupRepo.GetCiclesAsync(ct);
        return Ok(ApiResponse<IEnumerable<CicleDto>>.Ok(cicles.Select(MaparCicle)));
    }

    /// <summary>Crea un cicle nou.</summary>
    [HttpPost("cicles")]
    public async Task<ActionResult<ApiResponse<CicleDto>>> CrearCicle(
        [FromBody] CrearCicleDto dto, CancellationToken ct)
    {
        var cicle = new Cicle { Nom = dto.Nom.Trim(), Ordre = dto.Ordre };
        await _grupRepo.AfegirCicleAsync(cicle, ct);
        await _grupRepo.SaveChangesAsync(ct);

        var cicleAmbCursos = await _grupRepo.GetCiclePerIdAsync(cicle.Id, ct);
        return CreatedAtAction(nameof(GetCicles), ApiResponse<CicleDto>.Ok(MaparCicle(cicleAmbCursos!)));
    }

    /// <summary>Actualitza el nom i ordre d'un cicle.</summary>
    [HttpPut("cicles/{id:guid}")]
    public async Task<ActionResult<ApiResponse>> ActualitzarCicle(
        Guid id, [FromBody] ActualitzarCicleDto dto, CancellationToken ct)
    {
        var cicle = await _grupRepo.GetCiclePerIdAsync(id, ct);
        if (cicle == null) return NotFound(ApiResponse.Fail("Cicle no trobat"));

        cicle.Nom = dto.Nom.Trim();
        cicle.Ordre = dto.Ordre;
        await _grupRepo.ActualitzarCicleAsync(cicle, ct);
        await _grupRepo.SaveChangesAsync(ct);

        return Ok(ApiResponse.Ok());
    }

    /// <summary>Elimina un cicle sense cursos associats.</summary>
    [HttpDelete("cicles/{id:guid}")]
    public async Task<ActionResult<ApiResponse>> EsborrarCicle(Guid id, CancellationToken ct)
    {
        var cicle = await _grupRepo.GetCiclePerIdAsync(id, ct);
        if (cicle == null) return NotFound(ApiResponse.Fail("Cicle no trobat"));

        if (cicle.Cursos.Any(c => !c.IsDeleted))
            return BadRequest(ApiResponse.Fail("No es pot esborrar un cicle amb cursos actius"));

        await _grupRepo.EsborrarCicleAsync(id, ct);
        await _grupRepo.SaveChangesAsync(ct);

        return Ok(ApiResponse.Ok());
    }

    /// <summary>Crea un curs dins d'un cicle.</summary>
    [HttpPost("cicles/{cicleId:guid}/cursos")]
    public async Task<ActionResult<ApiResponse<CursDto>>> CrearCurs(
        Guid cicleId, [FromBody] CrearCursDto dto, CancellationToken ct)
    {
        var cicle = await _grupRepo.GetCiclePerIdAsync(cicleId, ct);
        if (cicle == null) return NotFound(ApiResponse<CursDto>.Fail("Cicle no trobat"));

        var curs = new Curs
        {
            CicleId = cicleId,
            Nom = dto.Nom.Trim(),
            Codi = dto.Codi.Trim().ToUpper(),
            Ordre = dto.Ordre,
            UsaModeFusteta = dto.UsaModeFusteta,
        };
        await _grupRepo.AfegirCursAsync(curs, ct);
        await _grupRepo.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetCicles), ApiResponse<CursDto>.Ok(new CursDto
        {
            Id = curs.Id,
            CicleId = curs.CicleId,
            Nom = curs.Nom,
            Codi = curs.Codi,
            Ordre = curs.Ordre,
            UsaModeFusteta = curs.UsaModeFusteta,
        }));
    }

    /// <summary>Actualitza les dades d'un curs.</summary>
    [HttpPut("cicles/{cicleId:guid}/cursos/{id:guid}")]
    public async Task<ActionResult<ApiResponse>> ActualitzarCurs(
        Guid cicleId, Guid id, [FromBody] ActualitzarCursDto dto, CancellationToken ct)
    {
        var curs = await _grupRepo.GetCursPerIdAsync(id, ct);
        if (curs == null || curs.CicleId != cicleId)
            return NotFound(ApiResponse.Fail("Curs no trobat"));

        curs.Nom = dto.Nom.Trim();
        curs.Codi = dto.Codi.Trim().ToUpper();
        curs.Ordre = dto.Ordre;
        curs.UsaModeFusteta = dto.UsaModeFusteta;
        await _grupRepo.ActualitzarCursAsync(curs, ct);
        await _grupRepo.SaveChangesAsync(ct);

        return Ok(ApiResponse.Ok());
    }

    /// <summary>Elimina un curs sense grups associats.</summary>
    [HttpDelete("cicles/{cicleId:guid}/cursos/{id:guid}")]
    public async Task<ActionResult<ApiResponse>> EsborrarCurs(
        Guid cicleId, Guid id, CancellationToken ct)
    {
        var curs = await _grupRepo.GetCursPerIdAsync(id, ct);
        if (curs == null || curs.CicleId != cicleId)
            return NotFound(ApiResponse.Fail("Curs no trobat"));

        if (curs.Grups.Any(g => !g.IsDeleted))
            return BadRequest(ApiResponse.Fail("No es pot esborrar un curs amb grups actius"));

        await _grupRepo.EsborrarCursAsync(id, ct);
        await _grupRepo.SaveChangesAsync(ct);

        return Ok(ApiResponse.Ok());
    }

    // ── Grups ─────────────────────────────────────────────────

    /// <summary>Llista els grups de l'any acadèmic actiu.</summary>
    [HttpGet("grups")]
    public async Task<ActionResult<ApiResponse<IEnumerable<GrupDto>>>> GetGrups(CancellationToken ct)
    {
        var anyActiu = await _calendariRepo.GetAnyAcademicActiuAsync(ct);
        if (anyActiu == null)
            return Ok(ApiResponse<IEnumerable<GrupDto>>.Ok([]));

        var grups = await _grupRepo.GetPerAnyAcademicAsync(anyActiu.Id, ct);
        return Ok(ApiResponse<IEnumerable<GrupDto>>.Ok(grups.Select(MaparGrup)));
    }

    /// <summary>Crea un grup nou per a l'any acadèmic actiu.</summary>
    [HttpPost("grups")]
    public async Task<ActionResult<ApiResponse<GrupDto>>> CrearGrup(
        [FromBody] CrearGrupDto dto, CancellationToken ct)
    {
        var anyActiu = await _calendariRepo.GetAnyAcademicActiuAsync(ct);
        if (anyActiu == null)
            return BadRequest(ApiResponse<GrupDto>.Fail("No hi ha cap any acadèmic actiu"));

        var grup = new Grup
        {
            CursId = dto.CursId,
            Lletra = dto.Lletra.ToUpper(),
            AnyAcademicId = anyActiu.Id,
            TutorId = dto.TutorId,
        };

        await _grupRepo.AfegirAsync(grup, ct);
        await _grupRepo.SaveChangesAsync(ct);

        var grupAmbDetalls = await _grupRepo.GetByIdAmbDetallsAsync(grup.Id, ct);
        _logger.LogInformation("Grup creat: {Nom} ({AnyAcademic})", grup.Lletra, anyActiu.Nom);
        return CreatedAtAction(nameof(GetGrups), ApiResponse<GrupDto>.Ok(MaparGrup(grupAmbDetalls!)));
    }

    /// <summary>Actualitza la lletra o el tutor d'un grup.</summary>
    [HttpPut("grups/{id:guid}")]
    public async Task<ActionResult<ApiResponse>> ActualitzarGrup(
        Guid id, [FromBody] ActualitzarGrupDto dto, CancellationToken ct)
    {
        var grup = await _grupRepo.GetByIdAsync(id, ct);
        if (grup == null) return NotFound(ApiResponse.Fail("Grup no trobat"));

        grup.Lletra = dto.Lletra.ToUpper();
        grup.TutorId = dto.TutorId;
        await _grupRepo.ActualitzarAsync(grup, ct);
        await _grupRepo.SaveChangesAsync(ct);

        return Ok(ApiResponse.Ok());
    }

    /// <summary>Elimina (soft delete) un grup sense alumnes actius.</summary>
    [HttpDelete("grups/{id:guid}")]
    public async Task<ActionResult<ApiResponse>> EsborrarGrup(Guid id, CancellationToken ct)
    {
        var grup = await _grupRepo.GetByIdAmbDetallsAsync(id, ct);
        if (grup == null) return NotFound(ApiResponse.Fail("Grup no trobat"));

        if (grup.Alumnes?.Any(a => a.EsActiu) == true)
            return BadRequest(ApiResponse.Fail("No es pot esborrar un grup amb alumnes actius"));

        await _grupRepo.EsborrarAsync(id, ct);
        await _grupRepo.SaveChangesAsync(ct);

        return Ok(ApiResponse.Ok());
    }

    // ── Helpers ─────────────────────────────────────────────

    private static CicleDto MaparCicle(Cicle c) => new()
    {
        Id = c.Id,
        Nom = c.Nom,
        Ordre = c.Ordre,
        Cursos = c.Cursos.Where(cu => !cu.IsDeleted).Select(cu => new CursDto
        {
            Id = cu.Id,
            CicleId = cu.CicleId,
            Nom = cu.Nom,
            Codi = cu.Codi,
            Ordre = cu.Ordre,
            UsaModeFusteta = cu.UsaModeFusteta,
        }).OrderBy(cu => cu.Ordre).ToList()
    };

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

    private static AnyAcademicDto MaparAnyAcademic(AnyAcademic a) => new()
    {
        Id = a.Id,
        Nom = a.Nom,
        DataInici = a.DataInici,
        DataFi = a.DataFi,
        EsActiu = a.EsActiu,
        IniciFT1 = a.IniciT1,
        FiT1 = a.FiT1,
        IniciFT2 = a.IniciT2,
        FiT2 = a.FiT2,
        IniciFT3 = a.IniciT3,
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
        var buffer = new byte[12];
        System.Security.Cryptography.RandomNumberGenerator.Fill(buffer);
        return new string(buffer.Select(b => chars[b % chars.Length]).ToArray());
    }
}
