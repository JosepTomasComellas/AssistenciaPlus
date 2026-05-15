using AssistenciaPlus.Web.Models;
using System.Net.Http.Json;
using System.Text.Json;

namespace AssistenciaPlus.Web.Services;

public class AlumnesService
{
    private readonly HttpClient _http;
    private static readonly JsonSerializerOptions _opts = new() { PropertyNameCaseInsensitive = true };

    public AlumnesService(HttpClient http) => _http = http;

    public async Task<List<AlumneModel>> GetAlumnesPerGrupAsync(Guid grupId)
    {
        var result = await _http.GetFromJsonAsync<ApiResponse<List<AlumneModel>>>(
            $"alumnes?grupId={grupId}", _opts);
        return (result?.Data ?? []).OrderBy(a => a.OrdreFusteta).ThenBy(a => a.Cognom1).ToList();
    }

    public async Task<AlumneModel?> GetAlumneAsync(Guid id)
    {
        var result = await _http.GetFromJsonAsync<ApiResponse<AlumneModel>>(
            $"alumnes/{id}", _opts);
        return result?.Data;
    }

    public async Task<(bool Ok, string? Error)> CrearAlumneAsync(CrearAlumneModel model)
    {
        var response = await _http.PostAsJsonAsync("alumnes", model);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<AlumneModel>>(_opts);
        return (result?.Success == true, result?.Error);
    }

    public async Task<(bool Ok, string? Error)> ActualitzarAlumneAsync(Guid id, ActualitzarAlumneModel model)
    {
        var response = await _http.PutAsJsonAsync($"alumnes/{id}", model);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse>(_opts);
        return (result?.Success == true, result?.Error);
    }

    public async Task<(bool Ok, string? Error)> EsborrarAlumneAsync(Guid id)
    {
        var response = await _http.DeleteAsync($"alumnes/{id}");
        var result = await response.Content.ReadFromJsonAsync<ApiResponse>(_opts);
        return (result?.Success == true, result?.Error);
    }
}
