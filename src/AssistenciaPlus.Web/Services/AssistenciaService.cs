using AssistenciaPlus.Web.Models;
using System.Net.Http.Json;
using System.Text.Json;

namespace AssistenciaPlus.Web.Services;

public class AssistenciaService
{
    private readonly HttpClient _http;
    private static readonly JsonSerializerOptions _opts = new() { PropertyNameCaseInsensitive = true };

    public AssistenciaService(HttpClient http) => _http = http;

    /// <summary>
    /// Retorna tots els registres d'un grup per a una data concreta.
    /// </summary>
    public async Task<List<RegistreAssistenciaModel>> GetRegistresAsync(Guid grupId, DateOnly data)
    {
        var dataStr = data.ToString("yyyy-MM-dd");
        var result = await _http.GetFromJsonAsync<ApiResponse<List<RegistreAssistenciaModel>>>(
            $"assistencia?grupId={grupId}&data={dataStr}", _opts);
        return result?.Data ?? [];
    }

    /// <summary>
    /// Retorna el registre d'una franja concreta, o null si no existeix.
    /// </summary>
    public async Task<RegistreAssistenciaModel?> GetRegistreAsync(
        Guid grupId, Guid franjaId, DateOnly data)
    {
        var dataStr = data.ToString("yyyy-MM-dd");
        try
        {
            var result = await _http.GetFromJsonAsync<ApiResponse<RegistreAssistenciaModel>>(
                $"assistencia/{grupId}/{franjaId}/{dataStr}", _opts);
            return result?.Data;
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    /// <summary>
    /// Desa una sessió completa d'assistència (crea o actualitza).
    /// </summary>
    public async Task<(bool Ok, RegistreAssistenciaModel? Data, string? Error)> DesarSessioAsync(
        DesarSessioModel model)
    {
        var response = await _http.PostAsJsonAsync("assistencia", model);
        var result = await response.Content
            .ReadFromJsonAsync<ApiResponse<RegistreAssistenciaModel>>(_opts);

        if (result?.Success == true)
            return (true, result.Data, null);

        return (false, null, result?.Error ?? "Error al desar l'assistència");
    }
}
