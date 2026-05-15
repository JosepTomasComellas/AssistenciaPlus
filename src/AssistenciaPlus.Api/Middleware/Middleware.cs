using AssistenciaPlus.Core.Interfaces;
using Microsoft.AspNetCore.SignalR;
using System.Net;
using System.Security.Claims;
using System.Text.Json;

namespace AssistenciaPlus.Api.Middleware;

// ═══════════════════════════════════════════════════════════
//  GESTIÓ D'EXCEPCIONS GLOBAL
// ═══════════════════════════════════════════════════════════

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excepció no controlada: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var (statusCode, message) = exception switch
        {
            UnauthorizedAccessException => (HttpStatusCode.Unauthorized, "No autoritzat"),
            ArgumentException e => (HttpStatusCode.BadRequest, e.Message),
            KeyNotFoundException e => (HttpStatusCode.NotFound, e.Message),
            InvalidOperationException e => (HttpStatusCode.Conflict, e.Message),
            _ => (HttpStatusCode.InternalServerError, "S'ha produït un error intern")
        };

        context.Response.StatusCode = (int)statusCode;

        var response = new
        {
            status = (int)statusCode,
            message,
            timestamp = DateTime.UtcNow
        };

        await context.Response.WriteAsync(
            JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            })
        );
    }
}

// ═══════════════════════════════════════════════════════════
//  SERVEI D'USUARI ACTUAL
// ═══════════════════════════════════════════════════════════

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public Guid? UserId
    {
        get
        {
            var claim = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(claim, out var id) ? id : null;
        }
    }

    public string? Email => User?.FindFirst(ClaimTypes.Email)?.Value;
    public string? Role => User?.FindFirst(ClaimTypes.Role)?.Value;
    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;
}

// ═══════════════════════════════════════════════════════════
//  SIGNALR HUB - ASSISTÈNCIA EN TEMPS REAL
// ═══════════════════════════════════════════════════════════

/// <summary>
/// Hub SignalR per notificar en temps real quan un professor
/// actualitza l'assistència (visible per a l'equip directiu).
/// </summary>
public class AttendanceHub : Hub
{
    public async Task JoinGroup(string groupId)
        => await Groups.AddToGroupAsync(Context.ConnectionId, $"group:{groupId}");

    public async Task LeaveGroup(string groupId)
        => await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"group:{groupId}");

    public async Task NotifyAttendanceUpdated(string sessionId, string groupId)
        => await Clients.OthersInGroup($"group:{groupId}")
            .SendAsync("AttendanceUpdated", sessionId);
}
