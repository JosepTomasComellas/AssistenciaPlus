using AssistenciaPlus.Web.Models;
using System.Net.Http.Json;
using System.Text.Json;

namespace AssistenciaPlus.Web.Services;

public class GrupService
{
    private readonly HttpClient _http;
    private static readonly JsonSerializerOptions _opts = new() { PropertyNameCaseInsensitive = true };

    public GrupService(HttpClient http) => _http = http;

    public async Task<List<GrupModel>> GetGrupsAsync()
    {
        var result = await _http.GetFromJsonAsync<ApiResponse<List<GrupModel>>>("grups", _opts);
        return result?.Data ?? [];
    }

    public async Task<List<FranjaHorariaModel>> GetFranjesAsync()
    {
        var result = await _http.GetFromJsonAsync<ApiResponse<List<FranjaHorariaModel>>>("grups/franjes", _opts);
        return (result?.Data ?? []).OrderBy(f => f.Ordre).ToList();
    }

    public async Task<List<CicleModel>> GetCiclesAsync()
    {
        var result = await _http.GetFromJsonAsync<ApiResponse<List<CicleModel>>>("grups/cicles", _opts);
        return (result?.Data ?? []).OrderBy(c => c.Ordre).ToList();
    }
}
