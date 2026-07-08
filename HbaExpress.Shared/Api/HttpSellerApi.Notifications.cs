using System.Net.Http.Json;

namespace Seller_MP_Dashboard.Api;

// Implémentation HTTP du domaine Notifications (/seller/notifications).
public partial class HttpSellerApi
{
    public async Task<IReadOnlyList<SellerNotification>> ListNotificationsAsync()
        => (await _http.GetFromJsonAsync<List<SellerNotification>>("/seller/notifications")) ?? new();

    public Task MarkNotificationReadAsync(Guid id)
        => _http.PostAsync($"/seller/notifications/{id}/read", null);

    public Task MarkAllNotificationsReadAsync()
        => _http.PostAsync("/seller/notifications/read-all", null);

    public Task RegisterDeviceAsync(string token, string platform)
        => _http.PostAsJsonAsync("/seller/notifications/devices", new { token, platform });

    public Task UnregisterDeviceAsync(string token)
        => _http.PostAsJsonAsync("/seller/notifications/devices/unregister", new { token });
}
