using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AssistenciaPlus.Api.Controllers;

[ApiController]
public abstract class BaseApiController : ControllerBase
{
    protected Guid GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(claim))
            throw new UnauthorizedAccessException("El token no conté l'identificador d'usuari.");
        return Guid.Parse(claim);
    }

    protected bool TryGetCurrentUserId(out Guid userId)
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(claim)) { userId = Guid.Empty; return false; }
        return Guid.TryParse(claim, out userId);
    }
}
