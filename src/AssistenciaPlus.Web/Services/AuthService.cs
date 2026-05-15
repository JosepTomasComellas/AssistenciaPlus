using AssistenciaPlus.Web.Auth;
using AssistenciaPlus.Web.Models;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
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
        catch (Exception ex)
        {
            return (false, $"Error de connexió: {ex.Message}");
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
