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
    public DateTime UploadedAt { get; set; }
}

[JsonConverter(typeof(TolerantEnumConverter<KybStatus>))]
public enum KybStatus { NotStarted, Pending, Submitted, Verified, Rejected }

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
public record CreateOfferRequest(Guid ProductId, string Sku, double BasePriceAmount, string Currency,
    string Condition, string FulfillmentType, Guid ShipFromLocationId, int HandlingTime);
public record OfferPriceRequest(double Amount, string Currency);
public record OfferStatusRequest(string Status);
public record HandlingTimeRequest(int HandlingTime);

public class Offer
{
    public Guid Id { get; init; }
    public Guid ProductId { get; init; }
    public string ProductName { get; set; } = "";
    public string Sku { get; set; } = "";
    public double BasePriceAmount { get; set; }
    public string Currency { get; set; } = "XOF";
    public string Condition { get; set; } = "new";       // new | used | refurbished
    public string FulfillmentType { get; set; } = "seller"; // seller | platform
    public int HandlingTime { get; set; }                 // jours
    public string Status { get; set; } = "active";        // active | paused | closed
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
    public int RefundsXof { get; init; }
    public int NetEarningsXof => GrossSalesXof - CommissionXof - RefundsXof;
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

[JsonConverter(typeof(TolerantEnumConverter<PayoutStatus>))]
public enum PayoutStatus { Pending, Processing, Paid, Failed }

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
