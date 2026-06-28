namespace Seller_MP_Dashboard.Models;

/// <summary>
/// Image d'un produit. Correspond à un média côté BFF
/// (POST /seller/products/{id}/media · MediaRequest).
/// En démo, l'emoji sert de visuel ; Url portera l'image réelle.
/// </summary>
public class ProductImage
{
    /// <summary>Identifiant du média côté catalogue (pour principale / ordre / suppression).</summary>
    public Guid Id { get; init; }
    public string? Url { get; init; }
    public string Emoji { get; init; } = "";
    public string? AltText { get; init; }
    public bool IsPrimary { get; init; }
}

/// <summary>Déclinaison (variante) d'un produit.</summary>
public class ProductVariant
{
    public Guid Id { get; init; }
    public string Sku { get; init; } = "";
    public string? Barcode { get; init; }
    public int WeightGrams { get; init; }
    public Dictionary<string, string> Attributes { get; init; } = new();

    public string AttributesLabel => Attributes.Count == 0
        ? "—"
        : string.Join(", ", Attributes.Select(a => $"{a.Key}: {a.Value}"));
}
