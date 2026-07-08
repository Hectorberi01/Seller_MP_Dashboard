using Microsoft.AspNetCore.Components;
using Seller_MP_Dashboard.Api;
using Seller_MP_Dashboard.Models;
using Seller_MP_Dashboard.Services;

namespace Seller_MP_Dashboard.Pages;

public partial class Login
{
    [Inject] private ToastService Toast { get; set; } = default!;
    [Inject] private IPushRegistration Push { get; set; } = default!;

    private LoginModel _login = new();
    private bool _busy;
    private string? _error;
    private bool _showPassword;

    // Bascule l'affichage du mot de passe en clair (icône œil).
    private void TogglePassword() => _showPassword = !_showPassword;

    private async Task HandleLogin()
    {
        _busy = true; _error = null;
        try
        {
            var result = await Api.LoginAsync(new LoginRequest(_login.Email, _login.Password, string.IsNullOrWhiteSpace(_login.MfaCode) ? null : _login.MfaCode));
            await Auth.SetSessionAsync(result);

            if (!Auth.IsAuthenticated)
            {
                _error = "Connexion acceptée mais aucun jeton trouvé dans la réponse. Le format de /seller/auth/login diffère de l'attendu.";
                await Toast.Error(_error);
                return;
            }

            // Enregistrement push (best-effort, non bloquant) : demande la permission,
            // récupère le token FCM et l'envoie au BFF. No-op sur Web/desktop.
            _ = Push.RegisterAsync();

            await Toast.Success("Bienvenue sur votre espace vendeur 👋");
            Nav.NavigateTo("");
        }
        catch
        {
            _error = "Connexion impossible. Vérifiez vos identifiants et que le serveur est joignable.";
            await Toast.Error(_error);
        }
        finally { _busy = false; }
    }
}