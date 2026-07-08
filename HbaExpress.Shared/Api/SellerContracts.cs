using System.Text.Json.Serialization;

namespace Seller_MP_Dashboard.Api;

// ============================================================
// Contrats du BFF Vendeur (Marketplace.Bff.Seller)
// Les requêtes reflètent les schémas du BFF. Les réponses
// (non décrites dans l'OpenAPI) sont modélisées de façon
// raisonnable pour alimenter l'UI.
// ============================================================

// ---------- Auth ----------
public record LoginRequest(string Email, string Password, string? MfaCode = null);
public record RefreshRequest(string RefreshToken);
public record LogoutRequest(string RefreshToken);
public record AuthResult(string AccessToken, string RefreshToken, DateTime ExpiresAt, string SellerName);

// ---------- Boutique ----------
public record OnboardRequest(string ShopName);
public record ProfileRequest(string ShopName, string? LogoUrl, string? Description);
public record PayoutAccountRequest(string Provider, string AccountNumber, string AccountName);
public record KybDocumentRequest(string Type, string FileUrl);

public class Shop
{
    public Guid Id { get; init; }
    public string ShopName { get; set; } = "";
    public string? Description { get; set; }
    public string? LogoUrl { get; set; }
    public KybStatus KybStatus { get; set; }
    public PayoutAccount? Payout { get; set; }

    [JsonPropertyName("kybDocuments")]
    public List<KybDocument> Documents { get; set; } = new();
}

public class PayoutAccount
{
    public string Provider { get; set; } = "";
    public string AccountNumber { get; set; } = "";
    public string AccountName { get; set; } = "";
}

public class KybDocument
{
    public Guid Id { get; init; }
    public string Type { get; set; } = "";
    public string FileUrl { get; set; } = "";
    public KybStatus Status { get; set; }

    [JsonPropertyName("uploadedAtUtc")]
    public DateTime UploadedAt { get; set; }

    public DateTime? VerifiedAtUtc { get; set; }
}

[JsonConverter(typeof(TolerantEnumConverter<KybStatus>))]
public enum KybStatus { NotStarted, Pending, Submitted, Verified, Rejected, InReview }

// ---------- Catalogue ----------
public class SellerCategory
{
    public Guid Id { get; set; }
    public Guid? ParentId { get; set; }
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public string Path { get; set; } = "";
    public string Status { get; set; } = "";
    /// <summary>Libellé affiché : chemin complet si disponible, sinon le nom.</summary>
    public string Display => string.IsNullOrWhiteSpace(Path) ? Name : Path.Replace("/", " › ");
}

public class SellerBrand
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public string Status { get; set; } = "";
}

public record CreateProductRequest(Guid CategoryId, string Name, string? Description,
    Guid? BrandId = null, string? Gtin = null, string? Ean = null, Guid? ProductGroupId = null,
    Dictionary<string, string>? Attributes = null, List<string>? Tags = null);

public record UpdateProductRequest(string Name, string? Description,
    Guid? BrandId = null, string? Gtin = null, string? Ean = null, Guid? ProductGroupId = null,
    Dictionary<string, string>? Attributes = null, List<string>? Tags = null);

public record CatalogStatusRequest(string Status);
public record MediaRequest(string Url, string Type, string? AltText, bool IsPrimary, string? ExternalId = null);

public class CatalogProduct
{
    public Guid Id { get; init; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public string Category { get; set; } = "";
    public string Status { get; set; } = "draft"; // draft | active | archived
    public List<string> Tags { get; set; } = new();
}

// ---------- Offres ----------
// Le champ prix est le PRIX VENDEUR (net) — sérialisé « sellerPrice » pour matcher
// le BFF (/seller/offers), qui ajoute commission + frais provider pour le prix produit.
public record CreateOfferRequest(Guid ProductId, string Sku,
    [property: JsonPropertyName("sellerPrice")] double BasePriceAmount, string Currency,
    string Condition, string FulfillmentType, Guid ShipFromLocationId, int HandlingTime);
public record OfferPriceRequest([property: JsonPropertyName("sellerPrice")] double Amount, string Currency);
public record OfferStatusRequest(string Status);
public record HandlingTimeRequest(int HandlingTime);

public class Offer
{
    public Guid Id { get; init; }
    public Guid ProductId { get; init; }
    public string ProductName { get; set; } = "";
    public string Sku { get; set; } = "";
    // Prix produit payé par l'acheteur (prix vendeur + commission + frais provider).
    // Le BFF renvoie ce montant dans BasePriceAmount ; les décompositions ci-dessous
    // (sellerPrice, commissionAmount, providerFeeAmount, productPrice) sont fournies
    // par le BFF si disponibles, sinon recalculées par les propriétés dérivées.
    public double BasePriceAmount { get; set; }
    public string Currency { get; set; } = "XOF";
    public string Condition { get; set; } = "new";       // new | used | refurbished
    public string FulfillmentType { get; set; } = "seller"; // seller | platform
    public int HandlingTime { get; set; }                 // jours
    public string Status { get; set; } = "active";        // active | paused | closed

    // ---- Décomposition du prix (renvoyée par le BFF si disponible, désér. camelCase) ----
    /// <summary>Prix vendeur (net) — ce que le vendeur reçoit.</summary>
    public double? SellerPrice { get; set; }
    /// <summary>Montant de la commission plateforme (10%).</summary>
    public double? CommissionAmount { get; set; }
    /// <summary>Montant des frais provider (5%).</summary>
    public double? ProviderFeeAmount { get; set; }
    /// <summary>Prix produit payé par l'acheteur.</summary>
    public double? ProductPrice { get; set; }

    // ---- Valeurs effectives (BFF si présent, sinon calcul depuis le prix produit) ----
    /// <summary>Prix payé par l'acheteur : ProductPrice du BFF, sinon BasePriceAmount.</summary>
    public double EffectiveProductPrice => ProductPrice ?? BasePriceAmount;
    /// <summary>Prix vendeur : SellerPrice du BFF, sinon prix produit / 1,15.</summary>
    public double EffectiveSellerPrice =>
        SellerPrice ?? EffectiveProductPrice / OfferPricing.PriceMultiplier;
}

/// <summary>
/// Constantes de tarification (aperçu en direct côté dashboard, alignées sur la config
/// backend). Le vendeur saisit son prix vendeur (net) ; la plateforme ajoute une
/// commission puis des frais provider pour obtenir le prix payé par l'acheteur.
/// </summary>
public static class OfferPricing
{
    /// <summary>Taux de commission plateforme (10%).</summary>
    public const double CommissionRate = 0.10;
    /// <summary>Taux de frais provider (5%).</summary>
    public const double ProviderFeeRate = 0.05;
    /// <summary>Multiplicateur prix vendeur → prix produit (1,15).</summary>
    public const double PriceMultiplier = 1 + CommissionRate + ProviderFeeRate;

    public static double Commission(double sellerPrice) => sellerPrice * CommissionRate;
    public static double ProviderFee(double sellerPrice) => sellerPrice * ProviderFeeRate;
    public static double ProductPrice(double sellerPrice) => sellerPrice * PriceMultiplier;
}

// ---------- Exécution / Expéditions ----------
public record ShipRequest(string Carrier, string TrackingNumber);

public class Shipment
{
    public Guid Id { get; init; }
    public Guid OrderId { get; init; }
    public string OrderReference { get; set; } = "";
    public string Customer { get; set; } = "";
    public ShipmentStatus Status { get; set; }
    public string? Carrier { get; set; }
    public string? TrackingNumber { get; set; }
    public int ItemCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

[JsonConverter(typeof(TolerantEnumConverter<ShipmentStatus>))]
public enum ShipmentStatus { Pending, Prepared, Shipped, Delivered, Cancelled }

// ---------- Finances ----------
public class FinanceStatement
{
    public DateTime From { get; init; }
    public DateTime To { get; init; }
    public int GrossSalesXof { get; init; }
    public int CommissionXof { get; init; }
    public int ProviderFeeXof { get; init; }
    public int RefundsXof { get; init; }
    public int NetEarningsXof => GrossSalesXof - CommissionXof - ProviderFeeXof - RefundsXof;
    public List<StatementLine> Lines { get; init; } = new();
}

public class StatementLine
{
    public DateTime Date { get; init; }
    public string Label { get; init; } = "";
    public string Type { get; init; } = ""; // sale | commission | refund | payout
    public int AmountXof { get; init; }         // signé
}

public class Payout
{
    public Guid Id { get; init; }
    public int AmountXof { get; init; }
    public string Currency { get; init; } = "XOF";
    public PayoutStatus Status { get; init; }
    public DateTime RequestedAt { get; init; }
    public string Provider { get; init; } = "";
}

// Requested/Rejected couvrent le workflow « demande → validation admin » : sans eux,
// le TolerantEnumConverter retomberait sur Pending et masquerait un refus.
[JsonConverter(typeof(TolerantEnumConverter<PayoutStatus>))]
public enum PayoutStatus { Pending, Requested, Processing, Paid, Failed, Rejected }

// ---------- Messagerie ----------
public record SendRequest(string Body, List<string>? Attachments = null);

public class Conversation
{
    public Guid Id { get; init; }
    public string Customer { get; set; } = "";
    public string? Subject { get; set; }
    public string LastMessage { get; set; } = "";
    public DateTime LastAt { get; set; }
    public int Unread { get; set; }
}

public class Message
{
    public Guid Id { get; init; }
    public bool FromSeller { get; init; }
    public string Body { get; init; } = "";
    public DateTime SentAt { get; init; }
}

// ---------- Avis ----------
public class Review
{
    public Guid Id { get; init; }
    public Guid ProductId { get; init; }
    public string Author { get; set; } = "";
    public int Rating { get; set; } // 1..5
    public string Body { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public string? SellerReply { get; set; }
    public bool Flagged { get; set; }
}

public class ProductRating
{
    public Guid ProductId { get; init; }
    public double Average { get; init; }
    public int Count { get; init; }
    /// <summary>Répartition par note : index 0 = 1★ … index 4 = 5★.</summary>
    public int[] Breakdown { get; init; } = new int[5];
}
