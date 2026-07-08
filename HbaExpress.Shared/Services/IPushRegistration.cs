namespace Seller_MP_Dashboard.Services;

/// <summary>
/// Pont vers l'enregistrement push (implémenté par l'app MAUI via Firebase).
/// Défini dans le RCL partagé pour que les pages (Login, déconnexion) puissent
/// l'appeler sans référencer MAUI/Firebase. Sur le Web/desktop, un no-op est utilisé.
/// </summary>
public interface IPushRegistration
{
    /// <summary>Demande la permission, récupère le token FCM et l'enregistre au BFF.</summary>
    Task RegisterAsync();

    /// <summary>Retire le token d'appareil (à la déconnexion).</summary>
    Task UnregisterAsync();
}

/// <summary>Implémentation par défaut (Web/desktop) : ne fait rien.</summary>
public sealed class NullPushRegistration : IPushRegistration
{
    public Task RegisterAsync() => Task.CompletedTask;
    public Task UnregisterAsync() => Task.CompletedTask;
}
