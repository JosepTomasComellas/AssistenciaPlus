using AssistenciaPlus.Web.Models;
using System.Net.Http.Json;
using System.Text.Json;

namespace AssistenciaPlus.Web.Services;

public class ConfiguracioService
{
    private readonly HttpClient _http;
    private static readonly JsonSerializerOptions _opts = new() { PropertyNameCaseInsensitive = true };

    public ConfiguracioService(HttpClient http) => _http = http;

    // ── Usuaris ──────────────────────────────────────────────

    public async Task<List<UsuariModel>> GetUsuarisAsync()
    {
        var result = await _http.GetFromJsonAsync<ApiResponse<List<UsuariModel>>>(
            "configuracio/usuaris", _opts);
        return result?.Data ?? [];
    }

    public async Task<(bool Ok, string? Error)> CrearUsuariAsync(CrearUsuariModel model)
    {
        var response = await _http.PostAsJsonAsync("configuracio/usuaris", model);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<UsuariModel>>(_opts);
        return (result?.Success == true, result?.Error);
    }

    public async Task<(bool Ok, string? Error)> ActualitzarUsuariAsync(Guid id, ActualitzarUsuariModel model)
    {
        var response = await _http.PutAsJsonAsync($"configuracio/usuaris/{id}", model);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse>(_opts);
        return (result?.Success == true, result?.Error);
    }

    public async Task<(bool Ok, string? Error)> EsborrarUsuariAsync(Guid id)
    {
        var response = await _http.DeleteAsync($"configuracio/usuaris/{id}");
        var result = await response.Content.ReadFromJsonAsync<ApiResponse>(_opts);
        return (result?.Success == true, result?.Error);
    }

    public async Task<(bool Ok, string? Error)> EnviarCredencialsAsync(Guid id)
    {
        var response = await _http.PostAsync($"configuracio/usuaris/{id}/enviar-credencials", null);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse>(_opts);
        return (result?.Success == true, result?.Error);
    }
}
