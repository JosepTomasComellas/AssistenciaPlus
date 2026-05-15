using AssistenciaPlus.Web.Models;
using Microsoft.AspNetCore.Components.Forms;
using System.Net.Http.Json;
using System.Text.Json;

namespace AssistenciaPlus.Web.Services;

public class ConfiguracioService
{
    private readonly HttpClient _http;
    private static readonly JsonSerializerOptions _opts = new() { PropertyNameCaseInsensitive = true };

    public ConfiguracioService(HttpClient http) => _http = http;

    // ── Anys acadèmics ───────────────────────────────────────

    public async Task<List<AnyAcademicModel>> GetAnysAcademicsAsync()
    {
        var result = await _http.GetFromJsonAsync<ApiResponse<List<AnyAcademicModel>>>(
            "configuracio/anys-academics", _opts);
        return result?.Data ?? [];
    }

    public async Task<(bool Ok, string? Error)> CrearAnyAcademicAsync(CrearAnyAcademicModel model)
    {
        var response = await _http.PostAsJsonAsync("configuracio/anys-academics", model);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<AnyAcademicModel>>(_opts);
        return (result?.Success == true, result?.Error);
    }

    public async Task<(bool Ok, string? Error)> ActivarAnyAcademicAsync(Guid id)
    {
        var response = await _http.PostAsync($"configuracio/anys-academics/{id}/activar", null);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse>(_opts);
        return (result?.Success == true, result?.Error);
    }

    // ── Cicles i cursos ──────────────────────────────────────

    public async Task<List<CicleModel>> GetCiclesAsync()
    {
        var result = await _http.GetFromJsonAsync<ApiResponse<List<CicleModel>>>(
            "configuracio/cicles", _opts);
        return result?.Data ?? [];
    }

    public async Task<(bool Ok, string? Error)> CrearCicleAsync(CrearCicleModel model)
    {
        var response = await _http.PostAsJsonAsync("configuracio/cicles", model);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<CicleModel>>(_opts);
        return (result?.Success == true, result?.Error);
    }

    public async Task<(bool Ok, string? Error)> ActualitzarCicleAsync(Guid id, ActualitzarCicleModel model)
    {
        var response = await _http.PutAsJsonAsync($"configuracio/cicles/{id}", model);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse>(_opts);
        return (result?.Success == true, result?.Error);
    }

    public async Task<(bool Ok, string? Error)> EsborrarCicleAsync(Guid id)
    {
        var response = await _http.DeleteAsync($"configuracio/cicles/{id}");
        var result = await response.Content.ReadFromJsonAsync<ApiResponse>(_opts);
        return (result?.Success == true, result?.Error);
    }

    public async Task<(bool Ok, string? Error)> CrearCursAsync(Guid cicleId, CrearCursModel model)
    {
        var response = await _http.PostAsJsonAsync($"configuracio/cicles/{cicleId}/cursos", model);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<CursModel>>(_opts);
        return (result?.Success == true, result?.Error);
    }

    public async Task<(bool Ok, string? Error)> ActualitzarCursAsync(Guid cicleId, Guid id, ActualitzarCursModel model)
    {
        var response = await _http.PutAsJsonAsync($"configuracio/cicles/{cicleId}/cursos/{id}", model);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse>(_opts);
        return (result?.Success == true, result?.Error);
    }

    public async Task<(bool Ok, string? Error)> EsborrarCursAsync(Guid cicleId, Guid id)
    {
        var response = await _http.DeleteAsync($"configuracio/cicles/{cicleId}/cursos/{id}");
        var result = await response.Content.ReadFromJsonAsync<ApiResponse>(_opts);
        return (result?.Success == true, result?.Error);
    }

    // ── Grups ────────────────────────────────────────────────

    public async Task<List<GrupModel>> GetGrupsConfiguracioAsync()
    {
        var result = await _http.GetFromJsonAsync<ApiResponse<List<GrupModel>>>(
            "configuracio/grups", _opts);
        return result?.Data ?? [];
    }

    public async Task<(bool Ok, string? Error)> CrearGrupAsync(CrearGrupModel model)
    {
        var response = await _http.PostAsJsonAsync("configuracio/grups", model);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<GrupModel>>(_opts);
        return (result?.Success == true, result?.Error);
    }

    public async Task<(bool Ok, string? Error)> ActualitzarGrupAsync(Guid id, ActualitzarGrupModel model)
    {
        var response = await _http.PutAsJsonAsync($"configuracio/grups/{id}", model);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse>(_opts);
        return (result?.Success == true, result?.Error);
    }

    public async Task<(bool Ok, string? Error)> EsborrarGrupAsync(Guid id)
    {
        var response = await _http.DeleteAsync($"configuracio/grups/{id}");
        var result = await response.Content.ReadFromJsonAsync<ApiResponse>(_opts);
        return (result?.Success == true, result?.Error);
    }

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

    public async Task<(bool Ok, string? FotoPath, string? Error)> PujarFotoUsuariAsync(
        Guid id, IBrowserFile fitxer)
    {
        using var content = new MultipartFormDataContent();
        var streamContent = new StreamContent(fitxer.OpenReadStream(maxAllowedSize: 2 * 1024 * 1024));
        streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(fitxer.ContentType);
        content.Add(streamContent, "foto", fitxer.Name);

        var response = await _http.PutAsync($"configuracio/usuaris/{id}/foto", content);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<string>>(_opts);
        return (result?.Success == true, result?.Data, result?.Error);
    }
}
