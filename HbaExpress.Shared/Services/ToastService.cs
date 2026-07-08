using Microsoft.JSInterop;

namespace Seller_MP_Dashboard.Services;

/// <summary>
/// Passerelle vers la couche « fun » JS (js/hbafx.js) : notifications flottantes
/// (toasts) et confettis. Injectable dans n'importe quelle page/composant.
/// </summary>
public sealed class ToastService
{
    private readonly IJSRuntime _js;

    public ToastService(IJSRuntime js) => _js = js;

    /// <summary>Toast de succès (vert).</summary>
    public ValueTask Success(string message) => Show(message, "success");

    /// <summary>Toast d'erreur (rouge).</summary>
    public ValueTask Error(string message) => Show(message, "error");

    /// <summary>Toast informatif (bleu).</summary>
    public ValueTask Info(string message) => Show(message, "info");

    /// <summary>Affiche un toast (type : success | error | info).</summary>
    public ValueTask Show(string message, string type = "info")
        => SafeInvoke("hbafx.toast", message, type);

    /// <summary>Déclenche une pluie de confettis (succès marquant).</summary>
    public ValueTask Celebrate() => SafeInvoke("hbafx.celebrate");

    // N'échoue jamais l'appelant si l'interop JS n'est pas prête (prérendu, etc.).
    private async ValueTask SafeInvoke(string identifier, params object?[] args)
    {
        try { await _js.InvokeVoidAsync(identifier, args); }
        catch { /* couche cosmétique : on ignore toute erreur d'interop */ }
    }
}
