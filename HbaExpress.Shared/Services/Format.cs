using System.Globalization;

namespace Seller_MP_Dashboard.Services;

/// <summary>Formatage cohérent des montants et dates pour le marché (XOF).</summary>
public static class Format
{
    private static readonly NumberFormatInfo Nfi = new()
    {
        NumberGroupSeparator = " ", // espace insécable
        NumberDecimalDigits = 0
    };

    /// <summary>Ex. 12 500 XOF (chiffres tabulaires côté CSS).</summary>
    public static string Xof(int amount) => amount.ToString("N0", Nfi) + " XOF";

    public static string Xof(double amount) => amount.ToString("N0", Nfi) + " XOF";

    public static string Date(DateTime d) => d.ToString("dd MMM yyyy", new CultureInfo("fr-FR"));

    public static string DateShort(DateTime d) => d.ToString("dd/MM", new CultureInfo("fr-FR"));
}
