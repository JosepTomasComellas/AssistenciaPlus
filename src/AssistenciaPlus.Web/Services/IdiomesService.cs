using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using System.Net.Http.Json;
using System.Text.Json;

namespace AssistenciaPlus.Web.Services;

public class IdiomesService
{
    private readonly HttpClient _http;
    private readonly ILocalStorageService _localStorage;
    private readonly NavigationManager _nav;

    private Dictionary<string, JsonElement> _traduccions = [];
    private string _idioma = "ca";

    public string Idioma => _idioma;
    public bool EsCatala => _idioma == "ca";

    public event Action? IdiomaCanviat;

    public IdiomesService(HttpClient http, ILocalStorageService localStorage, NavigationManager nav)
    {
        _http = http;
        _localStorage = localStorage;
        _nav = nav;
    }

    public async Task InicialitzarAsync()
    {
        var idiomaDesar = await _localStorage.GetItemAsStringAsync("idioma");
        _idioma = idiomaDesar ?? "ca";
        await CarregarTraduccionAsync();
    }

    public async Task CanviarIdiomaAsync(string idioma)
    {
        if (idioma != "ca" && idioma != "es") return;
        _idioma = idioma;
        await _localStorage.SetItemAsStringAsync("idioma", idioma);
        await CarregarTraduccionAsync();
        IdiomaCanviat?.Invoke();
    }

    public string T(string clau)
    {
        var parts = clau.Split('.');
        if (parts.Length != 2) return clau;

        if (_traduccions.TryGetValue(parts[0], out var seccio))
        {
            if (seccio.TryGetProperty(parts[1], out var valor))
                return valor.GetString() ?? clau;
        }
        return clau;
    }

    private async Task CarregarTraduccionAsync()
    {
        try
        {
            var url = _nav.BaseUri + $"i18n/{_idioma}.json";
            var json = await _http.GetFromJsonAsync<Dictionary<string, JsonElement>>(url);
            _traduccions = json ?? [];
        }
        catch
        {
            _traduccions = [];
        }
    }
}
