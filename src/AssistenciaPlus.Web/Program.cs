using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using AssistenciaPlus.Web;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var apiBaseUrl = builder.Configuration["ApiBaseUrl"]
    ?? builder.HostEnvironment.BaseAddress.TrimEnd('/') + "/api";

builder.Services.AddScoped(sp =>
    new HttpClient { BaseAddress = new Uri(apiBaseUrl) });

builder.Services.AddMudServices();

await builder.Build().RunAsync();
