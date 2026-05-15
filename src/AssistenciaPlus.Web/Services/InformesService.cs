using AssistenciaPlus.Web.Models;
using System.Net.Http.Json;
using System.Text.Json;

namespace AssistenciaPlus.Web.Services;

public class InformesService
{
    private readonly HttpClient _http;
    private static readonly JsonSerializerOptions _opts = new() { PropertyNameCaseInsensitive = true };

    public InformesService(HttpClient http) => _http = http;

    public async Task<InformeMensualModel?> GetInformeMensualGrupAsync(Guid grupId, int any, int mes)
    {
        var result = await _http.GetFromJsonAsync<ApiResponse<InformeMensualModel>>(
            $"informes/mensual/grup/{grupId}?any={any}&mes={mes}", _opts);
        return result?.Data;
    }

    public async Task<InformeTrimestralModel?> GetInformeTrimestralGrupAsync(
        Guid grupId, int trimestre, Guid anyAcademicId)
    {
        var result = await _http.GetFromJsonAsync<ApiResponse<InformeTrimestralModel>>(
            $"informes/trimestral/grup/{grupId}?trimestre={trimestre}&anyAcademicId={anyAcademicId}", _opts);
        return result?.Data;
    }

    public async Task<(bool Ok, string? Error)> EnviarInformeMensualAsync(
        Guid grupId, int any, int mes)
    {
        var dto = new EnviarInformeModel { GrupId = grupId, Any = any, Mes = mes };
        var response = await _http.PostAsJsonAsync("informes/enviar-mensual", dto);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse>(_opts);
        return (result?.Success == true, result?.Error);
    }
}
