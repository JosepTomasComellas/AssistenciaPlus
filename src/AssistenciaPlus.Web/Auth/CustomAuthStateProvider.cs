using AssistenciaPlus.Web.Models;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using System.Text.Json;

namespace AssistenciaPlus.Web.Auth;

public class CustomAuthStateProvider : AuthenticationStateProvider
{
    private readonly ILocalStorageService _localStorage;

    private static readonly AuthenticationState _anonymous =
        new(new ClaimsPrincipal(new ClaimsIdentity()));

    private static readonly JsonSerializerOptions _jsonOptions =
        new() { PropertyNameCaseInsensitive = true };

    public CustomAuthStateProvider(ILocalStorageService localStorage)
    {
        _localStorage = localStorage;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            var token = await _localStorage.GetItemAsStringAsync("jwt_token");
            if (string.IsNullOrEmpty(token))
                return _anonymous;

            var expiry = await _localStorage.GetItemAsync<DateTime>("token_expiry");
            if (DateTime.UtcNow > DateTime.SpecifyKind(expiry, DateTimeKind.Utc))
            {
                await ClearAsync();
                return _anonymous;
            }

            var userJson = await _localStorage.GetItemAsStringAsync("user_info");
            if (string.IsNullOrEmpty(userJson))
                return _anonymous;

            var user = JsonSerializer.Deserialize<UsuariModel>(userJson, _jsonOptions);
            if (user is null)
                return _anonymous;

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.NomComplet),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Rol),
                new Claim("idioma", user.Idioma)
            };

            return new AuthenticationState(
                new ClaimsPrincipal(new ClaimsIdentity(claims, "jwt")));
        }
        catch
        {
            return _anonymous;
        }
    }

    public void NotifyStateChanged() =>
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());

    public async Task ClearAsync()
    {
        await _localStorage.RemoveItemAsync("jwt_token");
        await _localStorage.RemoveItemAsync("token_expiry");
        await _localStorage.RemoveItemAsync("user_info");
    }
}
