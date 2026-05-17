using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.SignalR.Client;

namespace AssistenciaPlus.Web.Services;

public class SignalRService : IAsyncDisposable
{
    private readonly IWebAssemblyHostEnvironment _env;
    private readonly AuthService _authService;
    private HubConnection? _hub;

    public event Action<string, string>? AssistenciaActualitzada;
    public event Action? Reconnectant;
    public event Action? Reconnectat;
    public event Action? Desconnectat;

    public bool EstaConnectat => _hub?.State == HubConnectionState.Connected;

    public SignalRService(IWebAssemblyHostEnvironment env, AuthService authService)
    {
        _env = env;
        _authService = authService;
    }

    public async Task ConnectarAsync()
    {
        if (_hub != null) return;

        var token = await _authService.GetTokenAsync();
        var hubUrl = _env.BaseAddress.TrimEnd('/') + "/hubs/assistencia";

        _hub = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                options.AccessTokenProvider = () => Task.FromResult<string?>(token);
            })
            .WithAutomaticReconnect()
            .Build();

        _hub.On<string, string>("AssistenciaActualitzada", (franjaId, groupId) =>
        {
            AssistenciaActualitzada?.Invoke(franjaId, groupId);
        });

        _hub.Reconnecting += _ => { Reconnectant?.Invoke(); return Task.CompletedTask; };
        _hub.Reconnected += _ => { Reconnectat?.Invoke(); return Task.CompletedTask; };
        _hub.Closed += _ => { Desconnectat?.Invoke(); return Task.CompletedTask; };

        await _hub.StartAsync();
    }

    public async Task UnirseAlGrupAsync(Guid grupId)
    {
        if (_hub?.State == HubConnectionState.Connected)
            await _hub.InvokeAsync("JoinGroup", grupId.ToString());
    }

    public async Task AbandonarGrupAsync(Guid grupId)
    {
        if (_hub?.State == HubConnectionState.Connected)
            await _hub.InvokeAsync("LeaveGroup", grupId.ToString());
    }

    public async ValueTask DisposeAsync()
    {
        if (_hub != null)
        {
            await _hub.DisposeAsync();
            _hub = null;
        }
    }
}
