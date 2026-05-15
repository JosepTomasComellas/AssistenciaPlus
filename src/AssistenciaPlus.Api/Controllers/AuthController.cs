using AssistenciaPlus.Application.Common;
using AssistenciaPlus.Application.DTOs;
using AssistenciaPlus.Application.Interfaces;
using AssistenciaPlus.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace AssistenciaPlus.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : BaseApiController
{
    private readonly IUsuariRepository _usuariRepo;
    private readonly IConfiguration _config;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IUsuariRepository usuariRepo,
        IConfiguration config,
        ILogger<AuthController> logger)
    {
        _usuariRepo = usuariRepo;
        _config = config;
        _logger = logger;
    }

    /// <summary>Autenticació. Retorna un token JWT.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("login")]
    public async Task<ActionResult<ApiResponse<LoginResponseDto>>> Login(
        [FromBody] LoginRequestDto dto, CancellationToken ct)
    {
        var usuari = await _usuariRepo.GetPerEmailAsync(dto.Email, ct);

        if (usuari == null || !BCrypt.Net.BCrypt.Verify(dto.Contrasenya, usuari.PasswordHash))
        {
            _logger.LogWarning("Intent de login fallit per: {Email}", dto.Email);
            return Unauthorized(ApiResponse<LoginResponseDto>.Fail("Credencials incorrectes"));
        }

        usuari.UltimAcces = DateTime.UtcNow;
        await _usuariRepo.ActualitzarAsync(usuari, ct);
        await _usuariRepo.SaveChangesAsync(ct);

        var token = GenerarTokenJwt(usuari);
        var minutsExpiracio = int.Parse(_config["Jwt:ExpiryMinutes"] ?? "480");

        _logger.LogInformation("Login correcte: {Email} ({Rol})", usuari.Email, usuari.Rol);

        return Ok(ApiResponse<LoginResponseDto>.Ok(new LoginResponseDto
        {
            Token = token,
            Usuari = MaparUsuari(usuari),
            CaducarAt = DateTime.UtcNow.AddMinutes(minutsExpiracio)
        }));
    }

    /// <summary>Canvia la contrasenya de l'usuari autenticat.</summary>
    [HttpPost("canvi-contrasenya")]
    [Authorize]
    public async Task<ActionResult<ApiResponse>> CanviContrasenya(
        [FromBody] CanviContrasenyaDto dto, CancellationToken ct)
    {
        var usuariId = GetCurrentUserId();
        var usuari = await _usuariRepo.GetByIdAsync(usuariId, ct);
        if (usuari == null) return NotFound(ApiResponse.Fail("Usuari no trobat"));

        if (!BCrypt.Net.BCrypt.Verify(dto.ContrasenyaActual, usuari.PasswordHash))
            return BadRequest(ApiResponse.Fail("La contrasenya actual és incorrecta"));

        usuari.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.ContrasenyaNova, workFactor: 12);
        await _usuariRepo.ActualitzarAsync(usuari, ct);
        await _usuariRepo.SaveChangesAsync(ct);

        return Ok(ApiResponse.Ok());
    }

    /// <summary>Retorna les dades de l'usuari autenticat.</summary>
    [HttpGet("jo")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<UsuariDto>>> GetUsuariActual(CancellationToken ct)
    {
        var usuariId = GetCurrentUserId();
        var usuari = await _usuariRepo.GetByIdAsync(usuariId, ct);
        if (usuari == null) return NotFound(ApiResponse<UsuariDto>.Fail("Usuari no trobat"));

        return Ok(ApiResponse<UsuariDto>.Ok(MaparUsuari(usuari)));
    }

    // ── Helpers ─────────────────────────────────────────────

    private string GenerarTokenJwt(Usuari usuari)
    {
        var clau = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_config["Jwt:Secret"]!));
        var credencials = new SigningCredentials(clau, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, usuari.Id.ToString()),
            new Claim(ClaimTypes.Email, usuari.Email),
            new Claim(ClaimTypes.Name, usuari.NomComplet),
            new Claim(ClaimTypes.Role, usuari.Rol.ToString()),
            new Claim("idioma", usuari.Idioma)
        };

        var expiracio = int.Parse(_config["Jwt:ExpiryMinutes"] ?? "480");
        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiracio),
            signingCredentials: credencials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

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
}
