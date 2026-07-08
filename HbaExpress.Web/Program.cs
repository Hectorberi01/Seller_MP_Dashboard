using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Seller_MP_Dashboard;          // App (RCL partagée HbaExpress.Shared)
using Seller_MP_Dashboard.Services; // AuthState

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// URL du BFF Vendeur via wwwroot/appsettings.json : "Api": { "BaseUrl": ... }
var baseUrl = builder.Configuration["Api:BaseUrl"];
if (string.IsNullOrWhiteSpace(baseUrl))
{
    throw new InvalidOperationException(
        "Api:BaseUrl est requis (wwwroot/appsettings.json) : URL du BFF Vendeur.");
}

// Services partagés (mêmes enregistrements que l'hôte MAUI).
builder.Services.AddSellerDashboard(baseUrl);

// La restauration de session est faite dans App.razor (OnInitializedAsync),
// partagée entre l'hôte Web et l'hôte MAUI.
await builder.Build().RunAsync();
