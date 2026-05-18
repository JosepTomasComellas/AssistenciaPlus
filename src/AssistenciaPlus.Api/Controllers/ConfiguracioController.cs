using AssistenciaPlus.Api.Helpers;
using AssistenciaPlus.Application.Common;
using AssistenciaPlus.Application.DTOs;
using AssistenciaPlus.Application.Interfaces;
using AssistenciaPlus.Core.Interfaces;
using AssistenciaPlus.Domain.Entities;
using AssistenciaPlus.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

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
    private readonly IAlumneRepository _alumneRepo;
    private readonly IEmailService _emailService;
    private readonly IOllamaService _ollamaService;
    private readonly ImportacioAlumnesExcelService _importacioExcel;
    private readonly IConfiguration _config;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<ConfiguracioController> _logger;

    private static readonly string[] _mimesAcceptats = ["image/jpeg", "image/png", "image/webp"];
    private const long MidaMaxima = 2 * 1024 * 1024;

    public ConfiguracioController(
        ICalendariRepository calendariRepo,
        IGrupRepository grupRepo,
        IUsuariRepository usuariRepo,
        IAlumneRepository alumneRepo,
        IEmailService emailService,
        IOllamaService ollamaService,
        ImportacioAlumnesExcelService importacioExcel,
        IConfiguration config,
        IWebHostEnvironment env,
        ILogger<ConfiguracioController> logger)
    {
        _calendariRepo = calendariRepo;
        _grupRepo = grupRepo;
        _usuariRepo = usuariRepo;
        _alumneRepo = alumneRepo;
        _emailService = emailService;
        _ollamaService = ollamaService;
        _importacioExcel = importacioExcel;
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

    /// <summary>Importa dies festius des d'un fitxer .ics.</summary>
    [HttpPost("calendari/{anyAcademicId:guid}/importar-ics")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<ActionResult<ApiResponse<int>>> ImportarIcs(
        Guid anyAcademicId, IFormFile fitxer, CancellationToken ct)
    {
        var anyAcademic = await _calendariRepo.GetAnyAcademicPerIdAsync(anyAcademicId, ct);
        if (anyAcademic == null)
            return NotFound(ApiResponse<int>.Fail("Any acadèmic no trobat"));

        if (fitxer is null || fitxer.Length == 0)
            return BadRequest(ApiResponse<int>.Fail("Cal seleccionar un fitxer .ics"));

        string contingut;
        using (var reader = new StreamReader(fitxer.OpenReadStream()))
            contingut = await reader.ReadToEndAsync(ct);

        var events = ParsearIcs(contingut);
        var diesExistents = await _calendariRepo.GetDiesRangAsync(
            anyAcademicId, anyAcademic.DataInici, anyAcademic.DataFi, ct);
        var diesPer = diesExistents.ToDictionary(d => d.Data);

        var nous = new List<DiaCalendari>();
        var processats = new HashSet<DateOnly>();
        int importats = 0;

        foreach (var (dataInici, dataFi, resum) in events)
        {
            for (var data = dataInici; data < dataFi; data = data.AddDays(1))
            {
                if (data < anyAcademic.DataInici || data > anyAcademic.DataFi) continue;
                if (!processats.Add(data)) continue;

                if (diesPer.TryGetValue(data, out var diaExistent))
                {
                    diaExistent.TipusDia = TipusDia.Festiu;
                    diaExistent.Descripcio = resum;
                    await _calendariRepo.ActualitzarDiaAsync(diaExistent, ct);
                }
                else
                {
                    nous.Add(new DiaCalendari
                    {
                        AnyAcademicId = anyAcademicId,
                        Data = data,
                        TipusDia = TipusDia.Festiu,
                        Descripcio = resum
                    });
                }
                importats++;
            }
        }

        if (nous.Count > 0)
            await _calendariRepo.AfegirDiesAsync(nous, ct);
        await _calendariRepo.SaveChangesAsync(ct);

        _logger.LogInformation("ICS importat: {Count} festius per {Nom}", importats, anyAcademic.Nom);
        return Ok(ApiResponse<int>.Ok(importats));
    }

    /// <summary>Importa dies no lectius des d'un PDF de calendari escolar, usant Ollama.</summary>
    [HttpPost("calendari/{anyAcademicId:guid}/importar-pdf")]
    [RequestSizeLimit(20 * 1024 * 1024)]
    public async Task<ActionResult<ApiResponse<int>>> ImportarPdf(
        Guid anyAcademicId, IFormFile fitxer, CancellationToken ct)
    {
        var anyAcademic = await _calendariRepo.GetAnyAcademicPerIdAsync(anyAcademicId, ct);
        if (anyAcademic == null)
            return NotFound(ApiResponse<int>.Fail("Any acadèmic no trobat"));

        if (fitxer is null || fitxer.Length == 0)
            return BadRequest(ApiResponse<int>.Fail("Cal seleccionar un fitxer PDF"));

        if (!fitxer.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)
            && fitxer.ContentType != "application/pdf")
            return BadRequest(ApiResponse<int>.Fail("Cal seleccionar un fitxer .pdf"));

        string textPdf;
        try
        {
            using var stream = fitxer.OpenReadStream();
            textPdf = ExtreurTextPdf(stream);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error llegint el PDF del calendari");
            return BadRequest(ApiResponse<int>.Fail("No s'ha pogut llegir el fitxer PDF"));
        }

        if (string.IsNullOrWhiteSpace(textPdf) || textPdf.Length < 30)
            return BadRequest(ApiResponse<int>.Fail(
                "El PDF no conté text seleccionable. Prova exportant el calendari com a .ics des de Google Calendar."));

        if (!await _ollamaService.IsAvailableAsync(ct))
            return StatusCode(503, ApiResponse<int>.Fail(
                "El servei d'IA (Ollama) no està disponible. Comprova que el contenidor Ollama s'està executant."));

        var resposta = await _ollamaService.ParsearCalendariPdfAsync(textPdf, ct);
        var dates = ParsearRespostaOllamaCalendari(resposta);

        if (dates.Count == 0)
        {
            _logger.LogWarning("Ollama no ha extret cap data del PDF. Resposta: {R}", resposta[..Math.Min(200, resposta.Length)]);
            return Ok(ApiResponse<int>.Ok(0));
        }

        var diesExistents = await _calendariRepo.GetDiesRangAsync(
            anyAcademicId, anyAcademic.DataInici, anyAcademic.DataFi, ct);
        var diesPer = diesExistents.ToDictionary(d => d.Data);
        var nous = new List<DiaCalendari>();
        int importats = 0;

        foreach (var (data, tipusStr, descripcio) in dates)
        {
            if (data < anyAcademic.DataInici || data > anyAcademic.DataFi) continue;
            var tipusDia = tipusStr switch
            {
                "jornadaIntensiva" => TipusDia.JornadaIntensiva,
                "noLectiu" => TipusDia.NoLectiu,
                _ => TipusDia.Festiu
            };

            if (diesPer.TryGetValue(data, out var diaExistent))
            {
                diaExistent.TipusDia = tipusDia;
                diaExistent.Descripcio = descripcio;
                await _calendariRepo.ActualitzarDiaAsync(diaExistent, ct);
            }
            else
            {
                nous.Add(new DiaCalendari
                {
                    AnyAcademicId = anyAcademicId,
                    Data = data,
                    TipusDia = tipusDia,
                    Descripcio = descripcio
                });
            }
            importats++;
        }

        if (nous.Count > 0)
            await _calendariRepo.AfegirDiesAsync(nous, ct);
        await _calendariRepo.SaveChangesAsync(ct);

        _logger.LogInformation("PDF calendari importat via Ollama: {Count} dies per {Nom}", importats, anyAcademic.Nom);
        return Ok(ApiResponse<int>.Ok(importats));
    }

    /// <summary>Elimina un dia especial del calendari (el retorna a Lectiu).</summary>
    [HttpDelete("calendari/dia/{anyAcademicId:guid}/{data}")]
    public async Task<ActionResult<ApiResponse>> EsborrarDia(
        Guid anyAcademicId, string data, CancellationToken ct)
    {
        if (!DateOnly.TryParseExact(data, "yyyy-MM-dd", null,
                System.Globalization.DateTimeStyles.None, out var dataValida))
            return BadRequest(ApiResponse.Fail("Format de data invàlid (usa yyyy-MM-dd)"));

        await _calendariRepo.EsborrarDiaAsync(anyAcademicId, dataValida, ct);
        await _calendariRepo.SaveChangesAsync(ct);
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
        {
            try { System.IO.File.Delete(antic); } catch (IOException) { }
        }

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

    // ── Mestres autoritzats per grup ────────────────────────

    /// <summary>Llista els mestres autoritzats d'un grup (a més del tutor).</summary>
    [HttpGet("grups/{grupId:guid}/mestres")]
    public async Task<ActionResult<ApiResponse<IEnumerable<UsuariDto>>>> GetMestresGrup(
        Guid grupId, CancellationToken ct)
    {
        var mestres = await _grupRepo.GetMestresGrupAsync(grupId, ct);
        return Ok(ApiResponse<IEnumerable<UsuariDto>>.Ok(mestres.Select(MaparUsuari)));
    }

    /// <summary>Afegeix un mestre autoritzat a un grup.</summary>
    [HttpPost("grups/{grupId:guid}/mestres/{mestreId:guid}")]
    public async Task<ActionResult<ApiResponse>> AfegirMestreGrup(
        Guid grupId, Guid mestreId, CancellationToken ct)
    {
        var grup = await _grupRepo.GetByIdAsync(grupId, ct);
        if (grup == null) return NotFound(ApiResponse.Fail("Grup no trobat"));

        var mestre = await _usuariRepo.GetByIdAsync(mestreId, ct);
        if (mestre == null) return NotFound(ApiResponse.Fail("Usuari no trobat"));

        if (grup.TutorId == mestreId)
            return BadRequest(ApiResponse.Fail("Ja és el tutor d'aquest grup"));

        await _grupRepo.AfegirMestreGrupAsync(grupId, mestreId, ct);
        await _grupRepo.SaveChangesAsync(ct);

        _logger.LogInformation("Mestre {Mestre} afegit al grup {Grup}", mestre.NomComplet, grup.NomComplet);
        return Ok(ApiResponse.Ok());
    }

    /// <summary>Treu un mestre autoritzat d'un grup.</summary>
    [HttpDelete("grups/{grupId:guid}/mestres/{mestreId:guid}")]
    public async Task<ActionResult<ApiResponse>> TreureMestreGrup(
        Guid grupId, Guid mestreId, CancellationToken ct)
    {
        await _grupRepo.TreureMestreGrupAsync(grupId, mestreId, ct);
        await _grupRepo.SaveChangesAsync(ct);
        return Ok(ApiResponse.Ok());
    }

    // ── Importació d'alumnes Excel ────────────────────────────

    /// <summary>Importa alumnes des d'un fitxer Excel (format Esfera/Alexia) per a un grup.</summary>
    [HttpPost("grups/{grupId:guid}/importar-alumnes-excel")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<ActionResult<ApiResponse<ImportarAlumnesResultDto>>> ImportarAlumnesExcel(
        Guid grupId, IFormFile fitxer, CancellationToken ct)
    {
        var grup = await _grupRepo.GetByIdAsync(grupId, ct);
        if (grup == null) return NotFound(ApiResponse<ImportarAlumnesResultDto>.Fail("Grup no trobat"));

        if (fitxer is null || fitxer.Length == 0)
            return BadRequest(ApiResponse<ImportarAlumnesResultDto>.Fail("Cal seleccionar un fitxer Excel"));

        if (!fitxer.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase)
            && !fitxer.FileName.EndsWith(".xls", StringComparison.OrdinalIgnoreCase))
            return BadRequest(ApiResponse<ImportarAlumnesResultDto>.Fail("Cal un fitxer .xlsx o .xls"));

        List<Alumne> alumnes;
        List<string> errors;
        try
        {
            using var stream = fitxer.OpenReadStream();
            (alumnes, errors) = _importacioExcel.ParsearExcel(stream, grupId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error llegint Excel per al grup {GrupId}", grupId);
            return BadRequest(ApiResponse<ImportarAlumnesResultDto>.Fail("No s'ha pogut llegir el fitxer Excel"));
        }

        int importats = 0;
        foreach (var alumne in alumnes)
        {
            await _alumneRepo.AfegirAsync(alumne, ct);
            importats++;
        }
        if (importats > 0)
            await _alumneRepo.SaveChangesAsync(ct);

        _logger.LogInformation("Importats {Count} alumnes al grup {Grup}", importats, grup.NomComplet);
        return Ok(ApiResponse<ImportarAlumnesResultDto>.Ok(new ImportarAlumnesResultDto
        {
            Importats = importats,
            Ignorats = errors.Count(e => e.Contains("ignorat")),
            Errors = errors
        }));
    }

    // ── Migració d'any acadèmic ──────────────────────────────

    /// <summary>
    /// Copia tots els grups (i opcionalment els alumnes) de l'any acadèmic indicat cap a un altre.
    /// Útil per a iniciar un nou curs sense tornar a crear tots els grups manualment.
    /// </summary>
    [HttpPost("anys-academics/{anyAcademicOrigenId:guid}/migrar")]
    public async Task<ActionResult<ApiResponse<ResultatMigracioDto>>> MigrarAnyAcademic(
        Guid anyAcademicOrigenId, [FromBody] MigrarAnyAcademicDto dto, CancellationToken ct)
    {
        var anyOrigen = await _calendariRepo.GetAnyAcademicPerIdAsync(anyAcademicOrigenId, ct);
        if (anyOrigen == null) return NotFound(ApiResponse<ResultatMigracioDto>.Fail("Any acadèmic origen no trobat"));

        var anyDesti = await _calendariRepo.GetAnyAcademicPerIdAsync(dto.NouAnyAcademicId, ct);
        if (anyDesti == null) return NotFound(ApiResponse<ResultatMigracioDto>.Fail("Any acadèmic destí no trobat"));

        if (anyAcademicOrigenId == dto.NouAnyAcademicId)
            return BadRequest(ApiResponse<ResultatMigracioDto>.Fail("L'origen i el destí no poden ser el mateix any acadèmic"));

        var grupsOrigen = await _grupRepo.GetPerAnyAcademicAsync(anyAcademicOrigenId, ct);

        // Grups ja existents al destí (per evitar duplicats)
        var grupsDestiExistents = await _grupRepo.GetPerAnyAcademicAsync(dto.NouAnyAcademicId, ct);
        var clausDesti = grupsDestiExistents.Select(g => $"{g.CursId}_{g.Lletra}").ToHashSet();

        int grupsCopials = 0, alumnesCopials = 0;

        foreach (var grupOrigen in grupsOrigen)
        {
            var clau = $"{grupOrigen.CursId}_{grupOrigen.Lletra}";
            if (clausDesti.Contains(clau)) continue;

            var nouGrup = new Grup
            {
                CursId = grupOrigen.CursId,
                AnyAcademicId = dto.NouAnyAcademicId,
                Lletra = grupOrigen.Lletra,
                TutorId = grupOrigen.TutorId,
            };
            await _grupRepo.AfegirAsync(nouGrup, ct);
            await _grupRepo.SaveChangesAsync(ct);
            grupsCopials++;

            if (dto.IncloureAlumnes)
            {
                var alumnesOrigen = await _alumneRepo.GetPerGrupAsync(grupOrigen.Id, ct);
                foreach (var alumne in alumnesOrigen)
                {
                    var nouAlumne = new Alumne
                    {
                        Nom = alumne.Nom,
                        Cognom1 = alumne.Cognom1,
                        Cognom2 = alumne.Cognom2,
                        DataNaixement = alumne.DataNaixement,
                        GrupId = nouGrup.Id,
                        OrdreFusteta = alumne.OrdreFusteta,
                        EsActiu = true,
                    };
                    await _alumneRepo.AfegirAsync(nouAlumne, ct);
                    alumnesCopials++;
                }
                if (alumnesOrigen.Count > 0)
                    await _alumneRepo.SaveChangesAsync(ct);
            }
        }

        _logger.LogInformation(
            "Migració {Origen} → {Desti}: {Grups} grups, {Alumnes} alumnes",
            anyOrigen.Nom, anyDesti.Nom, grupsCopials, alumnesCopials);

        return Ok(ApiResponse<ResultatMigracioDto>.Ok(new ResultatMigracioDto
        {
            GrupsCopials = grupsCopials,
            AlumnesCopials = alumnesCopials
        }));
    }

    // ── Helpers ─────────────────────────────────────────────

    private static string ExtreurTextPdf(Stream stream)
    {
        using var document = UglyToad.PdfPig.PdfDocument.Open(stream);
        var sb = new System.Text.StringBuilder();
        foreach (var page in document.GetPages())
        {
            foreach (var word in page.GetWords())
                sb.Append(word.Text).Append(' ');
            sb.AppendLine();
        }
        return sb.ToString().Trim();
    }

    private static List<(DateOnly Data, string Tipus, string Descripcio)> ParsearRespostaOllamaCalendari(
        string resposta)
    {
        var result = new List<(DateOnly, string, string)>();
        try
        {
            var start = resposta.IndexOf('{');
            var end = resposta.LastIndexOf('}');
            if (start < 0 || end <= start) return result;

            using var doc = JsonDocument.Parse(resposta[start..(end + 1)]);
            if (!doc.RootElement.TryGetProperty("dies", out var dies)) return result;

            foreach (var dia in dies.EnumerateArray())
            {
                var dataStr = dia.TryGetProperty("data", out var d) ? d.GetString() : null;
                if (dataStr == null) continue;
                if (!DateOnly.TryParseExact(dataStr, "yyyy-MM-dd", null,
                        System.Globalization.DateTimeStyles.None, out var data))
                    continue;

                var tipus = dia.TryGetProperty("tipus", out var t) ? t.GetString() ?? "festiu" : "festiu";
                var desc = dia.TryGetProperty("descripcio", out var de) ? de.GetString() ?? "" : "";
                result.Add((data, tipus, desc));
            }
        }
        catch (JsonException) { }
        return result;
    }

    private static List<(DateOnly Start, DateOnly End, string Summary)> ParsearIcs(string content)
    {
        var events = new List<(DateOnly, DateOnly, string)>();
        var lines = content.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');

        var unfolded = new List<string>();
        foreach (var line in lines)
        {
            if (line.Length > 0 && (line[0] == ' ' || line[0] == '\t') && unfolded.Count > 0)
                unfolded[^1] += line[1..];
            else
                unfolded.Add(line.TrimEnd());
        }

        bool inEvent = false;
        DateOnly? dtStart = null, dtEnd = null;
        string summary = "";

        foreach (var line in unfolded)
        {
            if (line == "BEGIN:VEVENT") { inEvent = true; dtStart = dtEnd = null; summary = ""; }
            else if (line == "END:VEVENT" && inEvent)
            {
                if (dtStart.HasValue)
                    events.Add((dtStart.Value, dtEnd ?? dtStart.Value.AddDays(1), summary));
                inEvent = false;
            }
            else if (inEvent)
            {
                var ci = line.IndexOf(':');
                if (ci < 0) continue;
                var propRaw = line[..ci];
                var val = line[(ci + 1)..];
                var prop = propRaw.Contains(';') ? propRaw[..propRaw.IndexOf(';')] : propRaw;

                if (prop == "DTSTART") dtStart = ParsearDataIcs(val);
                else if (prop == "DTEND") dtEnd = ParsearDataIcs(val);
                else if (prop == "SUMMARY") summary = val.Trim();
            }
        }

        return events;
    }

    private static DateOnly? ParsearDataIcs(string value)
    {
        value = value.Trim();
        if (value.Length >= 8
            && int.TryParse(value[..4], out int y)
            && int.TryParse(value[4..6], out int m)
            && int.TryParse(value[6..8], out int d))
        {
            try { return new DateOnly(y, m, d); } catch { }
        }
        return null;
    }

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
        NombreAlumnes = g.Alumnes?.Count(a => a.EsActiu) ?? 0,
        MestresAutoritzats = g.MestresAutoritzats?.Select(m => new MestreGrupDto
        {
            UsuariId = m.UsuariId,
            NomComplet = m.Usuari?.NomComplet ?? string.Empty,
            Email = m.Usuari?.Email ?? string.Empty,
            AfegitAt = m.AfegitAt
        }).ToList() ?? []
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
