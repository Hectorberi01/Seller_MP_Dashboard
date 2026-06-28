using Microsoft.JSInterop;
using Seller_MP_Dashboard.Api;

namespace Seller_MP_Dashboard.Services;

/// <summary>
/// Conserve la session vendeur (jeton d'accès) et la persiste dans le
/// localStorage du navigateur, afin de survivre aux rafraîchissements de page.
/// Lu par BearerAuthHandler pour authentifier les appels au BFF.
/// </summary>
public class AuthState
{
    private const string KeyAccess = "mp_access";
    private const string KeyRefresh = "mp_refresh";
    private const string KeySeller = "mp_seller";
    private const string KeyExpires = "mp_expires";

    private readonly IJSRuntime _js;
    public AuthState(IJSRuntime js) => _js = js;

    public string? AccessToken { get; private set; }
    private string? RefreshToken { get;  set; }
    public string? SellerName { get; private set; }
    public DateTime? ExpiresAt { get; private set; }

    public bool IsAuthenticated =>
        !string.IsNullOrEmpty(AccessToken)
        && (ExpiresAt is null || ExpiresAt.Value.ToUniversalTime() > DateTime.UtcNow);

    public event Action? Changed;

    /// <summary>Restaure la session depuis le localStorage au démarrage de l'app.</summary>
    public async Task InitializeAsync()
    {
        try
        {
            AccessToken = await Get(KeyAccess);
            RefreshToken = await Get(KeyRefresh);
            SellerName = await Get(KeySeller);
            var exp = await Get(KeyExpires);
            ExpiresAt = DateTime.TryParse(exp, out var d) ? d : null;
            Changed?.Invoke();
        }
        catch { /* localStorage indisponible : session en mémoire uniquement */ }
    }

    public async Task SetSessionAsync(AuthResult result)
    {
        AccessToken = result.AccessToken;
        RefreshToken = result.RefreshToken;
        SellerName = result.SellerName;
        ExpiresAt = result.ExpiresAt;

        await Set(KeyAccess, AccessToken);
        await Set(KeyRefresh, RefreshToken);
        await Set(KeySeller, SellerName);
        await Set(KeyExpires, ExpiresAt?.ToString("O"));
        Changed?.Invoke();
    }

    public async Task ClearAsync()
    {
        AccessToken = RefreshToken = SellerName = null;
        ExpiresAt = null;

        foreach (var key in new[] { KeyAccess, KeyRefresh, KeySeller, KeyExpires })
            await _js.InvokeVoidAsync("localStorage.removeItem", key);

        Changed?.Invoke();
    }

    private async Task<string?> Get(string key)
        => await _js.InvokeAsync<string?>("localStorage.getItem", key);

    private async Task Set(string key, string? value)
    {
        if (string.IsNullOrEmpty(value))
            await _js.InvokeVoidAsync("localStorage.removeItem", key);
        else
            await _js.InvokeVoidAsync("localStorage.setItem", key, value);
    }
}
