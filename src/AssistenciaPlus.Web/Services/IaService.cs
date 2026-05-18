using AssistenciaPlus.Web.Models;
using System.Net.Http.Json;
using System.Text.Json;

namespace AssistenciaPlus.Web.Services;

public class IaService
{
    private readonly HttpClient _http;
    private static readonly JsonSerializerOptions _opts = new() { PropertyNameCaseInsensitive = true };

    public IaService(HttpClient http) => _http = http;

    public async Task<ConsultaIaModel> ConsultarAsync(ConsultaIaRequest request)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("ia/consulta", request);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<ConsultaIaModel>>(_opts);
            return result?.Data ?? new ConsultaIaModel { Resposta = "No s'ha pogut obtenir resposta.", EstaDisponible = false };
        }
        catch
        {
            return new ConsultaIaModel { Resposta = "Error de connexió amb el servei d'IA.", EstaDisponible = false };
        }
    }

    public async Task<bool> IsDisponibleAsync()
    {
        try
        {
            var result = await _http.GetFromJsonAsync<ApiResponse<bool>>("ia/disponible", _opts);
            return result?.Data == true;
        }
        catch
        {
            return false;
        }
    }
}
