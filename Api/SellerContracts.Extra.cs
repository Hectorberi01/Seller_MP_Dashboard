using System.Text.Json.Serialization;

namespace Seller_MP_Dashboard.Api;

// ============================================================
// Contrats additionnels (spec BFF élargi) :
// Compte, Catalogue (variants/suppression), Retours, Litiges, Stock.
// Réponses non décrites dans l'OpenAPI → modélisées au plus juste.
// ============================================================

// ---------- Compte ----------
public record AccountProfileRequest(string FirstName, string LastName, string? PhoneNumber);
public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
public record MfaCodeRequest(string Code);

public class AccountMe
{
    public Guid Id { get; init; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";
    public string? PhoneNumber { get; set; }
    public string Role { get; set; } = "Vendeur";
    public string? ShopName { get; set; }
    public bool MfaEnabled { get; set; }
    public DateTime CreatedAt { get; set; }

    public string FullName => $"{FirstName} {LastName}".Trim();
    public string Initials
    {
        get
        {
            var f = string.IsNullOrEmpty(FirstName) ? "" : FirstName[..1];
            var l = string.IsNullOrEmpty(LastName) ? "" : LastName[..1];
            var ini = (f + l).ToUpper();
            return string.IsNullOrEmpty(ini) ? "?" : ini;
        }
    }
}

public class MfaSetupResult
{
    public string SecretKey { get; init; } = "";
    public string? QrCodeUri { get; init; }
}

// ---------- Catalogue (étendu) ----------
public record VariantRequest(string Sku, Dictionary<string, string>? Attributes, string? Barcode,
    int WeightGrams, int? LengthMm = null, int? WidthMm = null, int? HeightMm = null);

// ---------- Retours ----------
public record ReturnRejectRequest(string Reason);
public record ReturnTrackingRequest(string Carrier, string TrackingNumber);
public record ReturnRefundRequest(double Amount);

[JsonConverter(typeof(TolerantEnumConverter<ReturnStatus>))]
public enum ReturnStatus { Requested, Approved, Rejected, InTransit, Received, Refunded }

public class Return
{
    public Guid Id { get; init; }
    public Guid OrderId { get; init; }
    public string OrderReference { get; set; } = "";
    public string Customer { get; set; } = "";
    public string Reason { get; set; } = "";
    public ReturnStatus Status { get; set; }
    public int AmountXof { get; set; }
    public DateTime RequestedAt { get; set; }
    public string? Carrier { get; set; }
    public string? TrackingNumber { get; set; }
}

// ---------- Litiges ----------
public record DisputeMessageRequest(string Body, string? PhotoUrl = null);

[JsonConverter(typeof(TolerantEnumConverter<DisputeStatus>))]
public enum DisputeStatus { Open, AwaitingSeller, AwaitingBuyer, Resolved, Closed }

public class Dispute
{
    public Guid Id { get; init; }
    public Guid OrderId { get; init; }
    public string OrderReference { get; set; } = "";
    public string Customer { get; set; } = "";
    public string Subject { get; set; } = "";
    public DisputeStatus Status { get; set; }
    public DateTime OpenedAt { get; set; }
    public List<DisputeMessage> Messages { get; set; } = new();
}

public class DisputeMessage
{
    public Guid Id { get; init; }
    public bool FromSeller { get; init; }
    public string Body { get; init; } = "";
    public string? PhotoUrl { get; init; }
    public DateTime SentAt { get; init; }
}

// ---------- Stock & lieux ----------
public record LocationRequest(string Line, string City, string Country, double? Latitude = null, double? Longitude = null);
public record CreateInventoryItemRequest(string Sku, Guid LocationId, int OnHand, int ReorderThreshold);
public record QuantityRequest(int Quantity);
public record DeltaRequest(int Delta);
public record ThresholdRequest(int Threshold);

public class Location
{
    public Guid Id { get; init; }
    public string Line { get; set; } = "";
    public string City { get; set; } = "";
    public string Country { get; set; } = "";
}

public class InventoryItem
{
    public Guid Id { get; init; }
    public string Sku { get; set; } = "";
    public Guid LocationId { get; init; }
    public int OnHand { get; set; }
    public int Reserved { get; set; }
    public int ReorderThreshold { get; set; }
    public bool IsLowStock { get; set; }
    public int Available => OnHand - Reserved;
}

public class InventoryAvailability
{
    public string Sku { get; init; } = "";
    public int Available { get; init; }
    public int OnHand { get; init; }
    public int Reserved { get; init; }
}

// ---------- Commandes (vendeur) ----------
// Désérialisé depuis /seller/orders (OrderSummary du BFF, déjà restreint aux
// lignes du vendeur). Expose des propriétés « prêtes UI » pour réutiliser les
// composants existants sans transformer le modèle de démo.
public class SellerOrder
{
    public Guid Id { get; set; }
    public Guid BuyerId { get; set; }
    public string Status { get; set; } = "";

    /// <summary>
    /// VRAI statut de paiement fourni par le BFF ("Paid" / "Pending" / "Refunded" /
    /// "Failed"), dérivé de la source de vérité Ordering. Champ additif, distinct de
    /// <see cref="Status"/> (statut commande/fulfillment). Null si le BFF ne le
    /// renvoie pas encore (compat ascendante → repli sur la dérivation depuis Status).
    /// </summary>
    [JsonPropertyName("paymentStatus")]
    public string? PaymentStatusRaw { get; set; }

    public DateTime CreatedAtUtc { get; set; }
    public decimal Subtotal { get; set; }
    public decimal GrandTotal { get; set; }
    public List<SellerOrderLine> Lines { get; set; } = new();

    /// <summary>Nom du client résolu par le BFF (via Identity). Vide sur la liste (non enrichie).</summary>
    [JsonPropertyName("customer")]
    public string? CustomerName { get; set; }

    /// <summary>Adresse de livraison figée sur la commande (null si non renseignée).</summary>
    public ShippingAddress? ShippingAddress { get; set; }

    public string Reference => "CMD-" + Id.ToString("N")[..8].ToUpperInvariant();

    /// <summary>Nom du client si le BFF l'a fourni (détail), sinon repli anonymisé.</summary>
    [JsonIgnore] // getter d'affichage : ne participe pas à la (dé)sérialisation ("customer" est déjà pris par CustomerName).
    public string Customer => string.IsNullOrWhiteSpace(CustomerName)
        ? "Client " + BuyerId.ToString("N")[..6].ToUpperInvariant()
        : CustomerName!;

    public DateTime Date => CreatedAtUtc;
    public int TotalXof => (int)Math.Round(GrandTotal);
    public int ItemCount => Lines.Sum(l => l.Quantity);

    public Seller_MP_Dashboard.Models.OrderStatus UiStatus => Status.ToLowerInvariant() switch
    {
        "delivered" => Seller_MP_Dashboard.Models.OrderStatus.Delivered,
        "cancelled" or "canceled" => Seller_MP_Dashboard.Models.OrderStatus.Cancelled,
        "shipped" => Seller_MP_Dashboard.Models.OrderStatus.Shipped,
        "confirmed" or "paid" or "processing" => Seller_MP_Dashboard.Models.OrderStatus.Processing,
        _ => Seller_MP_Dashboard.Models.OrderStatus.Pending
    };

    /// <summary>
    /// Statut de paiement pour l'UI. Priorité au VRAI statut de paiement fourni par
    /// le BFF (<see cref="PaymentStatusRaw"/>, source de vérité Ordering). Repli sur
    /// l'ancienne dérivation depuis <see cref="Status"/> si le champ est absent
    /// (compat ascendante). Note : "Failed" (BFF) n'a pas d'équivalent front → mappé
    /// sur Pending (paiement non encaissé → gating « Traiter » reste fermé).
    /// </summary>
    public Seller_MP_Dashboard.Models.PaymentStatus UiPayment =>
        string.IsNullOrWhiteSpace(PaymentStatusRaw)
            ? DerivePaymentFromStatus()
            : PaymentStatusRaw.ToLowerInvariant() switch
            {
                "paid" => Seller_MP_Dashboard.Models.PaymentStatus.Paid,
                "refunded" => Seller_MP_Dashboard.Models.PaymentStatus.Refunded,
                _ => Seller_MP_Dashboard.Models.PaymentStatus.Pending // "pending", "failed", inconnu
            };

    /// <summary>Ancienne dérivation (repli) : déduit le paiement du statut de commande.</summary>
    private Seller_MP_Dashboard.Models.PaymentStatus DerivePaymentFromStatus() => Status.ToLowerInvariant() switch
    {
        "paid" or "confirmed" or "shipped" or "delivered" => Seller_MP_Dashboard.Models.PaymentStatus.Paid,
        "refunded" => Seller_MP_Dashboard.Models.PaymentStatus.Refunded,
        _ => Seller_MP_Dashboard.Models.PaymentStatus.Pending
    };
}

// ---------- Tableau de bord (KPIs) ----------
public class SellerDashboardKpis
{
    public int OrdersTotal { get; set; }
    public int OrdersToProcess { get; set; }
    public decimal GrossSales30d { get; set; }
    public decimal NetPayout30d { get; set; }
    public string Currency { get; set; } = "XOF";
    public int ReviewsCount { get; set; }
    public double AverageRating { get; set; }
}

/// <summary>Adresse de livraison figée (snapshot BFF, cf. OrderShippingAddressSummary).</summary>
public class ShippingAddress
{
    public string? Label { get; set; }
    public string? Recipient { get; set; }
    public string? Line1 { get; set; }
    public string? Line2 { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public string? Phone { get; set; }

    /// <summary>Vrai si au moins une composante d'adresse est renseignée.</summary>
    public bool HasContent =>
        !string.IsNullOrWhiteSpace(Recipient) || !string.IsNullOrWhiteSpace(Line1) ||
        !string.IsNullOrWhiteSpace(City) || !string.IsNullOrWhiteSpace(Country);
}

public class SellerOrderLine
{
    public Guid ProductId { get; set; }
    public string Sku { get; set; } = "";
    public int Quantity { get; set; }
    public decimal FinalUnitPrice { get; set; }
    public decimal LineTotal { get; set; }

    /// <summary>Nom du produit résolu par le BFF (via Catalog). Repli sur le SKU si absent.</summary>
    [JsonPropertyName("productName")]
    public string? ProductNameResolved { get; set; }

    /// <summary>URL de l'image principale du produit (via Catalog). Null → repli emoji.</summary>
    public string? ImageUrl { get; set; }

    [JsonIgnore] // getter d'affichage ("productName" est déjà pris par ProductNameResolved).
    public string ProductName => !string.IsNullOrWhiteSpace(ProductNameResolved)
        ? ProductNameResolved!
        : (string.IsNullOrWhiteSpace(Sku) ? "Article" : Sku);

    public string Emoji => "📦";
    public int Qty => Quantity;
    public int UnitPriceXof => (int)Math.Round(FinalUnitPrice);
    public int LineTotalXof => (int)Math.Round(LineTotal);
}
