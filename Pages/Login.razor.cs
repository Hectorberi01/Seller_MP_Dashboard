using Seller_MP_Dashboard.Api;
using Seller_MP_Dashboard.Models;

namespace Seller_MP_Dashboard.Pages;

public partial class Login
{
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
                return;
            }

            Nav.NavigateTo("");
        }
        catch
        {
            _error = "Connexion impossible. Vérifiez vos identifiants et que le serveur est joignable.";
        }
        finally { _busy = false; }
    }
}