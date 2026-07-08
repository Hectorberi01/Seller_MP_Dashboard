using Seller_MP_Dashboard.Api;
using Plugin.Firebase.CloudMessaging;
using Plugin.Firebase.CloudMessaging.EventArgs;

namespace Seller_MP_Dashboard.Services;

public sealed class PushService : IPushRegistration
{
    private readonly ISellerApi _api;
    private bool _subscribed;

    public PushService(ISellerApi api) => _api = api;

    public async Task RegisterAsync()
    {
        // 1) Permission (iOS + Android 13+).
        await CrossFirebaseCloudMessaging.Current.CheckIfValidAsync();

        // 2) S'abonner AVANT de demander le token. Sur appareil réel, le token APNs
        //    (donc le token FCM) arrive de façon asynchrone et n'est souvent PAS
        //    encore prêt à cet instant : GetTokenAsync() renvoie null. Sans cet
        //    abonnement préalable, le token émis juste après serait perdu et
        //    l'appareil ne s'enregistrerait jamais (bug observé sur iPhone/TestFlight).
        if (!_subscribed)
        {
            CrossFirebaseCloudMessaging.Current.TokenChanged += OnTokenChanged;
            CrossFirebaseCloudMessaging.Current.NotificationTapped += OnNotificationTapped;
            _subscribed = true;
        }

        // 3) Tentative immédiate (souvent déjà disponible, ex. simulateur / relance).
        try
        {
            var token = await CrossFirebaseCloudMessaging.Current.GetTokenAsync();
            if (!string.IsNullOrWhiteSpace(token))
            {
                await SendTokenAsync(token);
            }
        }
        catch
        {
            // Pas grave : TokenChanged prendra le relais dès que le token est prêt.
        }
    }

    public async Task UnregisterAsync()
    {
        try
        {
            var token = await CrossFirebaseCloudMessaging.Current.GetTokenAsync();
            if (!string.IsNullOrWhiteSpace(token))
            {
                await _api.UnregisterDeviceAsync(token);
            }
        }
        catch
        {
            // best-effort
        }
    }

    // Émis quand le token FCM devient disponible ou change (rotation). C'est le
    // chemin fiable sur appareil réel : on enregistre le token dès son arrivée.
    private async void OnTokenChanged(object? sender, FCMTokenChangedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(e.Token))
        {
            await SendTokenAsync(e.Token);
        }
    }

    private void OnNotificationTapped(object? sender, FCMNotificationTappedEventArgs e)
    {
        // e.Notification.Data["type"], e.Notification.Data["entityId"]
        // → navigation selon le type (ex. "Order" → commande/{entityId}).
    }

    private Task SendTokenAsync(string token)
    {
        System.Diagnostics.Debug.WriteLine($"[Push] FCM token = {token}");
        var platform = OperatingSystem.IsIOS() ? "ios" : "android";
        return _api.RegisterDeviceAsync(token, platform);
    }
}
