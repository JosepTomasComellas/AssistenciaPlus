using AssistenciaPlus.Web.Auth;
using AssistenciaPlus.Web.Models;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace AssistenciaPlus.Web.Services;

public class AuthService
{
    private readonly HttpClient _http;
    private readonly ILocalStorageService _localStorage;
    private readonly CustomAuthStateProvider _authProvider;

    private static readonly JsonSerializerOptions _jsonOptions =
        new() { PropertyNameCaseInsensitive = true };

    public AuthService(
        HttpClient http,
        ILocalStorageService localStorage,
        AuthenticationStateProvider authProvider)
    {
        _http = http;
        _localStorage = localStorage;
        _authProvider = (CustomAuthStateProvider)authProvider;
    }

    /// <summary>Inicialitza el token al header si n'hi ha un emmagatzemat.</summary>
    public async Task InitializeAsync()
    {
        var token = await _localStorage.GetItemAsStringAsync("jwt_token");
        if (!string.IsNullOrEmpty(token))
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    /// <summary>Login. Retorna (true, null) si va bé o (false, missatgeError).</summary>
    public async Task<(bool Ok, string? Error)> LoginAsync(string email, string contrasenya)
    {
        try
        {
            var request = new LoginRequestModel { Email = email, Contrasenya = contrasenya };
            var response = await _http.PostAsJsonAsync("auth/login", request);

            var result = await response.Content
                .ReadFromJsonAsync<ApiResponse<LoginResponseModel>>(_jsonOptions);

            if (result?.Success != true || result.Data == null)
                return (false, result?.Error ?? "Error de connexió amb el servidor");

            await _localStorage.SetItemAsStringAsync("jwt_token", result.Data.Token);
            await _localStorage.SetItemAsync("token_expiry", result.Data.CaducarAt);
            await _localStorage.SetItemAsStringAsync("user_info",
                JsonSerializer.Serialize(result.Data.Usuari));

            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", result.Data.Token);

            _authProvider.NotifyStateChanged();
            return (true, null);
        }
        catch
        {
            return (false, "No s'ha pogut connectar amb el servidor. Comprova la connexió i torna-ho a intentar.");
        }
    }

    public async Task LogoutAsync()
    {
        await _authProvider.ClearAsync();
        _http.DefaultRequestHeaders.Authorization = null;
        _authProvider.NotifyStateChanged();
    }

    public async Task<UsuariModel?> GetCurrentUserAsync()
    {
        var json = await _localStorage.GetItemAsStringAsync("user_info");
        if (string.IsNullOrEmpty(json)) return null;
        return JsonSerializer.Deserialize<UsuariModel>(json, _jsonOptions);
    }

    /// <summary>Obté les dades fresques de l'usuari autenticat des de l'API i actualitza el localStorage.</summary>
    public async Task<UsuariModel?> GetCurrentUserFromApiAsync()
    {
        await InitializeAsync(); // Garantir que el token és al header
        try
        {
            var result = await _http.GetFromJsonAsync<ApiResponse<UsuariModel>>("auth/jo", _jsonOptions);
            if (result?.Success == true && result.Data != null)
            {
                await _localStorage.SetItemAsStringAsync("user_info",
                    JsonSerializer.Serialize(result.Data));
                return result.Data;
            }
        }
        catch { }
        return await GetCurrentUserAsync();
    }

    /// <summary>Puja o substitueix la foto de perfil de l'usuari autenticat.</summary>
    public async Task<(bool Ok, string? FotoPath, string? Error)> PujarFotoPerfilAsync(IBrowserFile fitxer)
    {
        try
        {
            using var contingut = new MultipartFormDataContent();
            var stream = fitxer.OpenReadStream(maxAllowedSize: 2 * 1024 * 1024 + 1024);
            using var streamContent = new StreamContent(stream);
            streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(fitxer.ContentType);
            contingut.Add(streamContent, "foto", fitxer.Name);

            var response = await _http.PutAsync("auth/foto", contingut);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<string>>(_jsonOptions);

            if (result?.Success == true && result.Data != null)
                return (true, result.Data, null);

            return (false, null, result?.Error ?? "Error al pujar la foto");
        }
        catch
        {
            return (false, null, "No s'ha pogut pujar la foto. Comprova la connexió.");
        }
    }

    public async Task<string?> GetTokenAsync()
        => await _localStorage.GetItemAsStringAsync("jwt_token");

    public async Task<bool> CanviarContrasenyaAsync(string actual, string nova)
    {
        var dto = new CanviContrasenyaModel
        {
            ContrasenyaActual = actual,
            ContrasenyaNova = nova
        };
        var response = await _http.PostAsJsonAsync("auth/canvi-contrasenya", dto);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse>(_jsonOptions);
        return result?.Success == true;
    }
}
