using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.LifecycleEvents;
#if IOS
using Plugin.Firebase.Bundled.Platforms.iOS;
#elif ANDROID
using Plugin.Firebase.Bundled.Platforms.Android;
#endif
using Seller_MP_Dashboard.Services;

namespace HbaExpress.MobileDesktop;

public static class MauiProgram
{
    // URL du BFF Vendeur appelée par l'app native. En Blazor Hybrid, le HttpClient
    // est le HttpClient .NET (handler natif) : aucune contrainte CORS ici.
    private const string ApiBaseUrl = "https://seller.marketplace-staging.hba-marketplace.fr";

    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();

        // Init Firebase (Plugin.Firebase v4) via les événements de cycle de vie.
        // C'est LE point d'init recommandé en v4 (pas AppDelegate/MainApplication).
        builder.ConfigureLifecycleEvents(events =>
        {
#if IOS
            events.AddiOS(ios => ios.WillFinishLaunching((_, __) =>
            {
                CrossFirebase.Initialize(CreateFirebaseSettings()); // iOS : settings uniquement
                // Indispensable en iOS pour activer le Cloud Messaging (token FCM).
                Plugin.Firebase.CloudMessaging.FirebaseCloudMessagingImplementation.Initialize();
                return false;
            }));
#elif ANDROID
            events.AddAndroid(android => android.OnCreate((activity, _) =>
                CrossFirebase.Initialize(activity, () => Microsoft.Maui.ApplicationModel.Platform.CurrentActivity, CreateFirebaseSettings())));
#endif
        });

        builder.Services.AddMauiBlazorWebView();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        // Mêmes services partagés que l'hôte Web (RCL HbaExpress.Shared).
        builder.Services.AddSellerDashboard(ApiBaseUrl);

        // Remplace le no-op push par l'implémentation Firebase (après AddSellerDashboard
        // pour que cet enregistrement l'emporte). PushService doit implémenter IPushRegistration.
        builder.Services.AddSingleton<IPushRegistration, PushService>();

        return builder.Build();
    }

#if IOS || ANDROID
    // Seul le Cloud Messaging (push) nous intéresse : les autres services Firebase
    // restent désactivés (pas de config supplémentaire à fournir).
    private static Plugin.Firebase.Bundled.Shared.CrossFirebaseSettings CreateFirebaseSettings()
        => new(isCloudMessagingEnabled: true);
#endif
}
