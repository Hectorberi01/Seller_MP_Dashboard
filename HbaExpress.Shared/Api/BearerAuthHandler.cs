using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Components;
using Seller_MP_Dashboard.Services;

namespace Seller_MP_Dashboard.Api;

/// <summary>
/// Ajoute « Authorization: Bearer {token} » sur toutes les routes vendeur (sauf
/// login/refresh). Maintient la session ouverte tant que l'utilisateur ne se
/// déconnecte pas :
///  • refresh PROACTIF si le jeton d'accès est expiré et qu'un jeton de
///    rafraîchissement existe ;
///  • sur 401, tente un refresh puis REJOUE la requête une fois ;
///  • si le refresh échoue (jeton de rafraîchissement invalide), alors seulement
///    on vide la session et on renvoie vers /login.
/// </summary>
public class BearerAuthHandler : DelegatingHandler
{
    private const string LoginPath = "/seller/auth/login";
    private const string RefreshPath = "/seller/auth/refresh";

    private readonly AuthState _auth;
    private readonly NavigationManager _nav;

    public BearerAuthHandler(AuthState auth, NavigationManager nav)
    {
        _auth = auth;
        _nav = nav;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var path = request.RequestUri?.AbsolutePath ?? string.Empty;
        var isAuthCall =
            path.EndsWith(LoginPath, StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(RefreshPath, StringComparison.OrdinalIgnoreCase);

        // 1) Refresh proactif : le jeton est expiré mais récupérable.
        if (!isAuthCall && _auth.HasRefreshToken && _auth.IsAccessTokenExpired)
            await _auth.RefreshTokenAsync();

        if (!isAuthCall && !string.IsNullOrEmpty(_auth.AccessToken))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _auth.AccessToken);

        // Bufferise le corps si un re-essai après refresh est possible (le contenu
        // est consommé par le 1er envoi et ne pourrait pas être relu autrement).
        byte[]? body = null;
        if (!isAuthCall && request.Content is not null && _auth.HasRefreshToken)
        {
            body = await request.Content.ReadAsByteArrayAsync(cancellationToken);
            var buffered = new ByteArrayContent(body);
            foreach (var h in request.Content.Headers)
                buffered.Headers.TryAddWithoutValidation(h.Key, h.Value);
            request.Content = buffered;
        }

        var response = await base.SendAsync(request, cancellationToken);

        // 2) 401 : on tente un refresh, puis on rejoue la requête une seule fois.
        if (response.StatusCode == HttpStatusCode.Unauthorized && !isAuthCall)
        {
            if (await _auth.RefreshTokenAsync(force: true))
            {
                var retry = new HttpRequestMessage(request.Method, request.RequestUri);
                foreach (var h in request.Headers)
                    retry.Headers.TryAddWithoutValidation(h.Key, h.Value);
                retry.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _auth.AccessToken);

                if (body is not null)
                {
                    var content = new ByteArrayContent(body);
                    if (request.Content is not null)
                        foreach (var h in request.Content.Headers)
                            content.Headers.TryAddWithoutValidation(h.Key, h.Value);
                    retry.Content = content;
                }

                response.Dispose();
                response = await base.SendAsync(retry, cancellationToken);
                if (response.StatusCode != HttpStatusCode.Unauthorized)
                    return response;
            }

            // Refresh impossible → session réellement terminée.
            await _auth.ClearAsync();
            _nav.NavigateTo("login");
        }

        return response;
    }
}
