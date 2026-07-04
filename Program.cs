using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Seller_MP_Dashboard;
using Seller_MP_Dashboard.Api;
using Seller_MP_Dashboard.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Session vendeur (jeton) partagée à toute l'app.
builder.Services.AddSingleton<AuthState>();

// --- BFF Vendeur (données réelles uniquement, aucun mock) ---
// URL du BFF via wwwroot/appsettings.json : "Api": { "BaseUrl": ... }
var baseUrl = builder.Configuration["Api:BaseUrl"];
if (string.IsNullOrWhiteSpace(baseUrl))
{
    throw new InvalidOperationException(
        "Api:BaseUrl est requis (wwwroot/appsettings.json) : URL du BFF Vendeur.");
}

// Appels HTTP au BFF avec en-tête Bearer.
builder.Services.AddTransient<BearerAuthHandler>();
builder.Services
    .AddHttpClient<ISellerApi, HttpSellerApi>(client => client.BaseAddress = new Uri(baseUrl))
    .AddHttpMessageHandler<BearerAuthHandler>();

var host = builder.Build();

// Restaure la session (jeton) depuis le localStorage avant le premier rendu.
await host.Services.GetRequiredService<AuthState>().InitializeAsync();

await host.RunAsync();