using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.JSInterop;
using Seller_MP_Dashboard.Api;

namespace Seller_MP_Dashboard.Services;

/// <summary>
/// Conserve la session vendeur (jeton d'accès + jeton de rafraîchissement) et la
/// persiste dans le localStorage, afin de survivre aux rafraîchissements de page
/// ET aux redémarrages de l'application. Le jeton d'accès (JWT court) est
/// automatiquement renouvelé via le jeton de rafraîchissement : l'utilisateur
/// reste connecté tant qu'il ne se déconnecte pas explicitement.
/// Lu par BearerAuthHandler pour authentifier les appels au BFF.
/// </summary>
public class AuthState
{
    /// <summary>Client HTTP « nu » (sans BearerAuthHandler) dédié au rafraîchissement.</summary>
    public const string AuthHttpClientName = "seller-auth";

    private const string KeyAccess = "mp_access";
    private const string KeyRefresh = "mp_refresh";
    private const string KeySeller = "mp_seller";
    private const string KeyExpires = "mp_expires";

    private readonly IJSRuntime _js;
    private readonly IHttpClientFactory _httpFactory;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    public AuthState(IJSRuntime js, IHttpClientFactory httpFactory)
    {
        _js = js;
        _httpFactory = httpFactory;
    }

    public string? AccessToken { get; private set; }
    private string? RefreshToken { get; set; }
    public string? SellerName { get; private set; }
    public DateTime? ExpiresAt { get; private set; }

    public bool HasRefreshToken => !string.IsNullOrEmpty(RefreshToken);

    /// <summary>Le jeton d'accès est expiré (ou sur le point de l'être, marge 30 s).</summary>
    public bool IsAccessTokenExpired =>
        ExpiresAt is { } e && e.ToUniversalTime() <= DateTime.UtcNow.AddSeconds(30);

    /// <summary>
    /// Session considérée active si le jeton d'accès est valide OU s'il existe un
    /// jeton de rafraîchissement (session récupérable). Le renouvellement effectif
    /// est fait par BearerAuthHandler / RefreshTokenAsync. Ainsi l'utilisateur n'est
    /// jamais éjecté tant qu'il n'a pas cliqué « Déconnexion ».
    /// </summary>
    public bool IsAuthenticated =>
        HasRefreshToken
        || (!string.IsNullOrEmpty(AccessToken)
            && (ExpiresAt is null || ExpiresAt.Value.ToUniversalTime() > DateTime.UtcNow));

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
        }
        catch { /* localStorage indisponible : session en mémoire uniquement */ }

        // Jeton d'accès expiré mais jeton de rafraîchissement présent : on renouvelle
        // dès le démarrage pour repartir avec un jeton frais (best-effort).
        if (IsAccessTokenExpired && HasRefreshToken)
        {
            try { await RefreshTokenAsync(); } catch { /* réessayé au 1er appel */ }
        }

        Changed?.Invoke();
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

    /// <summary>
    /// Renouvelle le jeton d'accès à partir du jeton de rafraîchissement.
    /// Retourne true si un jeton d'accès valide est désormais disponible.
    /// Sérialisé (verrou) : plusieurs appels concurrents ne déclenchent qu'un refresh.
    /// </summary>
    /// <param name="force">Forcer le renouvellement même si le jeton semble encore
    /// valide (cas d'un 401 : le serveur rejette un jeton que notre horloge croit bon).</param>
    public async Task<bool> RefreshTokenAsync(bool force = false)
    {
        if (string.IsNullOrEmpty(RefreshToken)) return false;

        await _refreshLock.WaitAsync();
        try
        {
            // Un appel concurrent a peut-être déjà renouvelé pendant l'attente.
            if (!force && !IsAccessTokenExpired && !string.IsNullOrEmpty(AccessToken)) return true;

            var client = _httpFactory.CreateClient(AuthHttpClientName);
            using var resp = await client.PostAsJsonAsync("/seller/auth/refresh", new RefreshRequest(RefreshToken!));
            if (!resp.IsSuccessStatusCode) return false;

            // Même parseur tolérant que le login (formats de champs variés).
            var result = HttpSellerApi.ParseAuth(await resp.Content.ReadAsStringAsync());
            if (string.IsNullOrEmpty(result.AccessToken)) return false;

            // Le refresh ne renvoie pas toujours le nom de boutique : on le conserve.
            if (string.IsNullOrEmpty(result.SellerName) && !string.IsNullOrEmpty(SellerName))
                result = result with { SellerName = SellerName };

            await SetSessionAsync(result);
            return true;
        }
        catch
        {
            return false; // réseau/serveur indisponible : on retentera plus tard
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    public async Task ClearAsync()
    {
        AccessToken = RefreshToken = SellerName = null;
        ExpiresAt = null;

        try
        {
            foreach (var key in new[] { KeyAccess, KeyRefresh, KeySeller, KeyExpires })
                await _js.InvokeVoidAsync("localStorage.removeItem", key);
        }
        catch { /* JS indisponible (hors contexte WebView, handler HTTP…) : sans effet */ }

        Changed?.Invoke();
    }

    // Persistance localStorage « best-effort » : le jeton vit en mémoire (Singleton) ;
    // si l'interop JS n'est pas disponible (MAUI Hybrid hors WebView, portée handler),
    // on n'échoue pas — on perd seulement la persistance entre lancements.
    private async Task<string?> Get(string key)
    {
        try { return await _js.InvokeAsync<string?>("localStorage.getItem", key); }
        catch { return null; }
    }

    private async Task Set(string key, string? value)
    {
        try
        {
            if (string.IsNullOrEmpty(value))
                await _js.InvokeVoidAsync("localStorage.removeItem", key);
            else
                await _js.InvokeVoidAsync("localStorage.setItem", key, value);
        }
        catch { /* JS indisponible : jeton conservé en mémoire uniquement */ }
    }
}
