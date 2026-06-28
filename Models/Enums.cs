namespace Seller_MP_Dashboard.Models;

public enum StockStatus
{
    InStock,    // En stock
    LowStock,   // Stock faible
    OutOfStock  // Rupture
}

public enum OrderStatus
{
    Pending,    // En attente
    Processing, // En préparation
    Shipped,    // Expédiée
    Delivered,  // Livrée
    Cancelled   // Annulée
}

public enum PaymentStatus
{
    Paid,       // Payé
    Pending,    // En attente
    Refunded    // Remboursé
}

/// <summary>Variantes visuelles de badge alignées sur la charte.</summary>
public enum BadgeVariant
{
    Stock,    // En stock — vert clair
    Promo,    // Promo -X% — ambre clair
    Verified, // Vendeur vérifié — bleu clair
    Rupture,  // Rupture — rouge clair
    Neutral   // Information neutre
}

/// <summary>Styles de bouton de la charte.</summary>
public enum ButtonVariant
{
    Action,     // Vert — une seule action d'achat par écran
    Primary,    // Bleu — action principale non commerciale
    Secondary,  // Contour bleu
    Tertiary,   // Texte bleu seul
    Danger      // Rouge — suppression
}
