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

    public async Task<(bool Ok, string? Error)> EnviarInformeTrimestralAsync(
        Guid grupId, int trimestre, Guid anyAcademicId)
    {
        var dto = new EnviarInformeTrimestralModel
        {
            GrupId = grupId,
            Trimestre = trimestre,
            AnyAcademicId = anyAcademicId
        };
        var response = await _http.PostAsJsonAsync("informes/enviar-trimestral", dto);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse>(_opts);
        return (result?.Success == true, result?.Error);
    }

    public async Task<(byte[]? Data, string? NomFitxer, string? Error)> ExportarExcelMensualAsync(
        Guid grupId, int any, int mes)
    {
        var response = await _http.GetAsync(
            $"informes/export-excel/mensual/grup/{grupId}?any={any}&mes={mes}");
        if (!response.IsSuccessStatusCode)
            return (null, null, "Error al generar l'Excel");

        var bytes = await response.Content.ReadAsByteArrayAsync();
        var nomFitxer = response.Content.Headers.ContentDisposition?.FileNameStar
            ?? response.Content.Headers.ContentDisposition?.FileName
            ?? $"informe_{mes}_{any}.xlsx";
        return (bytes, nomFitxer.Trim('"'), null);
    }

    public async Task<(byte[]? Data, string? NomFitxer, string? Error)> ExportarPdfAlumneAsync(
        Guid alumneId, Guid grupId, string tipusPeriode,
        int? any = null, int? mes = null, int? trimestre = null, Guid? anyAcademicId = null)
    {
        string url;
        if (tipusPeriode == "mensual")
            url = $"informes/pdf/alumne/{alumneId}?grupId={grupId}&tipusPeriode=mensual&any={any}&mes={mes}";
        else
            url = $"informes/pdf/alumne/{alumneId}?grupId={grupId}&tipusPeriode=trimestral&trimestre={trimestre}&anyAcademicId={anyAcademicId}";

        var response = await _http.GetAsync(url);
        if (!response.IsSuccessStatusCode)
            return (null, null, "Error al generar el PDF");

        var bytes = await response.Content.ReadAsByteArrayAsync();
        var nomFitxer = response.Content.Headers.ContentDisposition?.FileNameStar
            ?? response.Content.Headers.ContentDisposition?.FileName
            ?? "informe_alumne.pdf";
        return (bytes, nomFitxer.Trim('"'), null);
    }

    public async Task<(byte[]? Data, string? NomFitxer, string? Error)> ExportarExcelTrimestralAsync(
        Guid grupId, int trimestre, Guid anyAcademicId)
    {
        var response = await _http.GetAsync(
            $"informes/export-excel/trimestral/grup/{grupId}?trimestre={trimestre}&anyAcademicId={anyAcademicId}");
        if (!response.IsSuccessStatusCode)
            return (null, null, "Error al generar l'Excel");

        var bytes = await response.Content.ReadAsByteArrayAsync();
        var nomFitxer = response.Content.Headers.ContentDisposition?.FileNameStar
            ?? response.Content.Headers.ContentDisposition?.FileName
            ?? $"informe_T{trimestre}.xlsx";
        return (bytes, nomFitxer.Trim('"'), null);
    }
}
