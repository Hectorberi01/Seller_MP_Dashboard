using Seller_MP_Dashboard.Api;
using Seller_MP_Dashboard.Services;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Enregistrement des services partagés du dashboard vendeur, appelé à
/// l'identique par l'hôte Web (WASM) et l'hôte MAUI (Blazor Hybrid).
/// </summary>
public static class HbaExpressServiceCollectionExtensions
{
    /// <param name="apiBaseUrl">URL de base du BFF vendeur (ex. https://seller.…/).</param>
    public static IServiceCollection AddSellerDashboard(this IServiceCollection services, string apiBaseUrl)
    {
        // Session (jeton) : SINGLETON. En Blazor Hybrid, les handlers de
        // HttpClientFactory (BearerAuthHandler) vivent dans une portée différente
        // de l'UI ; il faut une instance unique pour que le jeton posé au login
        // soit visible par le handler. La persistance localStorage est best-effort
        // (voir AuthState : appels JS enveloppés de try/catch).
        services.AddSingleton<AuthState>();
        // Enregistrement push : no-op par défaut (Web/desktop). L'app MAUI le
        // remplace par une implémentation Firebase après cet appel.
        services.AddSingleton<IPushRegistration, NullPushRegistration>();
        // Couche « fun » : toasts + confettis (interop js/hbafx.js).
        services.AddScoped<ToastService>();

        // Client HTTP typé vers le BFF, avec en-tête Bearer.
        services.AddTransient<BearerAuthHandler>();
        services
            .AddHttpClient<ISellerApi, HttpSellerApi>(client => client.BaseAddress = new Uri(apiBaseUrl))
            .AddHttpMessageHandler<BearerAuthHandler>();

        // Client HTTP « nu » (SANS BearerAuthHandler) réservé au rafraîchissement du
        // jeton : évite toute récursion (le refresh ne doit pas repasser par le
        // handler qui déclencherait lui-même un refresh / une redirection 401).
        services.AddHttpClient(AuthState.AuthHttpClientName, client => client.BaseAddress = new Uri(apiBaseUrl));

        return services;
    }
}
