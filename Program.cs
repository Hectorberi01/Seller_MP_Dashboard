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

// Données mock encore utilisées par les écrans sans endpoint BFF
// (Vue d'ensemble / KPIs, liste Produits, liste Commandes).
builder.Services.AddSingleton<MockDataService>();
builder.Services.AddSingleton<MockSellerApi>();

// Session vendeur (jeton) partagée à toute l'app.
builder.Services.AddSingleton<AuthState>();

// --- BFF Vendeur ---
// Bascule via wwwroot/appsettings.json : "Api": { "UseMock": ..., "BaseUrl": ... }
var useMock = bool.TryParse(builder.Configuration["Api:UseMock"], out var m) && m;
var baseUrl = builder.Configuration["Api:BaseUrl"];

if (useMock || string.IsNullOrWhiteSpace(baseUrl))
{
    // Mode démo : tout passe par le mock.
    builder.Services.AddSingleton<ISellerApi>(sp => sp.GetRequiredService<MockSellerApi>());
}
else
{
    // Mode réel : appels HTTP au BFF avec en-tête Bearer.
    builder.Services.AddTransient<BearerAuthHandler>();
    builder.Services
        .AddHttpClient<ISellerApi, HttpSellerApi>(client => client.BaseAddress = new Uri(baseUrl))
        .AddHttpMessageHandler<BearerAuthHandler>();
}

var host = builder.Build();

// Restaure la session (jeton) depuis le localStorage avant le premier rendu.
await host.Services.GetRequiredService<AuthState>().InitializeAsync();

await host.RunAsync();