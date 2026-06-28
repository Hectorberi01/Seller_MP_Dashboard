using Seller_MP_Dashboard.Models;
using Seller_MP_Dashboard.Services;

namespace Seller_MP_Dashboard.Api;

/// <summary>
/// Implémentation en mémoire du BFF Vendeur. Aucune dépendance réseau :
/// sert de source de données pour la démo. À remplacer par HttpSellerApi.
/// </summary>
public partial class MockSellerApi : ISellerApi
{
    private readonly MockDataService _data;
    private static Guid G(int n) => new($"00000000-0000-0000-0000-{n:000000000000}");

    // ----- État en mémoire -----
    private readonly Shop _shop = new()
    {
        Id = G(1),
        ShopName = "AfriTech Store",
        Description = "Accessoires high-tech sélectionnés, livrés en 2-3 jours.",
        LogoUrl = null,
        KybStatus = KybStatus.Verified,
        Payout = new PayoutAccount { Provider = "Wave", AccountNumber = "•••• 4821", AccountName = "AfriTech SARL" },
        Documents = new()
        {
            new() { Id = G(101), Type = "Registre de commerce", FileUrl = "rccm.pdf", Status = KybStatus.Verified, UploadedAt = new(2026, 5, 12) },
            new() { Id = G(102), Type = "Pièce d'identité du gérant", FileUrl = "cni.pdf", Status = KybStatus.Verified, UploadedAt = new(2026, 5, 12) },
        }
    };

    private readonly List<Offer> _offers = new()
    {
        new() { Id = G(201), ProductId = G(11), ProductName = "Casque audio sans fil",       Sku = "CAS-001", BasePriceAmount = 12500, HandlingTime = 2, Status = "active", Condition = "new" },
        new() { Id = G(202), ProductId = G(12), ProductName = "Montre connectée sport",       Sku = "MTR-014", BasePriceAmount = 24900, HandlingTime = 3, Status = "active", Condition = "new" },
        new() { Id = G(203), ProductId = G(13), ProductName = "Enceinte Bluetooth portable",  Sku = "ENC-007", BasePriceAmount = 8900,  HandlingTime = 1, Status = "active", Condition = "new" },
        new() { Id = G(204), ProductId = G(14), ProductName = "Chargeur rapide 65W",          Sku = "CHG-022", BasePriceAmount = 6500,  HandlingTime = 2, Status = "paused", Condition = "new" },
        new() { Id = G(205), ProductId = G(15), ProductName = "Clavier mécanique compact",    Sku = "CLV-018", BasePriceAmount = 18500, HandlingTime = 2, Status = "active", Condition = "refurbished" },
    };

    private readonly List<Shipment> _shipments = new()
    {
        new() { Id = G(301), OrderId = G(401), OrderReference = "CMD-10428", Customer = "Aïssata Diallo",  Status = ShipmentStatus.Pending,   ItemCount = 2, CreatedAt = new(2026, 6, 24) },
        new() { Id = G(302), OrderId = G(402), OrderReference = "CMD-10427", Customer = "Kwame Mensah",    Status = ShipmentStatus.Prepared,  ItemCount = 1, CreatedAt = new(2026, 6, 24) },
        new() { Id = G(303), OrderId = G(403), OrderReference = "CMD-10426", Customer = "Fatou Ndiaye",    Status = ShipmentStatus.Shipped,   ItemCount = 3, Carrier = "DHL",   TrackingNumber = "DHL-558210", CreatedAt = new(2026, 6, 23) },
        new() { Id = G(304), OrderId = G(404), OrderReference = "CMD-10425", Customer = "Ibrahim Touré",   Status = ShipmentStatus.Delivered, ItemCount = 1, Carrier = "Chronopost", TrackingNumber = "CHR-110934", CreatedAt = new(2026, 6, 23) },
        new() { Id = G(305), OrderId = G(405), OrderReference = "CMD-10423", Customer = "Yao Konan",       Status = ShipmentStatus.Cancelled, ItemCount = 1, CreatedAt = new(2026, 6, 22) },
    };

    private readonly List<Conversation> _conversations = new()
    {
        new() { Id = G(501), Customer = "Aïssata Diallo", Subject = "Délai de livraison",     LastMessage = "Bonjour, quand sera expédiée ma commande ?", LastAt = new(2026, 6, 24, 9, 12, 0), Unread = 1 },
        new() { Id = G(502), Customer = "Fatou Ndiaye",   Subject = "Compatibilité produit",  LastMessage = "Merci pour votre réponse rapide !",          LastAt = new(2026, 6, 23, 16, 40, 0), Unread = 0 },
        new() { Id = G(503), Customer = "Cheikh Fall",    Subject = "Facture",                LastMessage = "Pouvez-vous m'envoyer la facture ?",          LastAt = new(2026, 6, 22, 11, 5, 0),  Unread = 2 },
    };

    private readonly Dictionary<Guid, List<Message>> _messages = new();
    private readonly List<Review> _reviews;

    public MockSellerApi(MockDataService data)
    {
        _data = data;
        _messages[G(501)] = new()
        {
            new() { Id = G(601), FromSeller = false, Body = "Bonjour, quand sera expédiée ma commande ?", SentAt = new(2026, 6, 24, 9, 12, 0) },
        };
        _messages[G(502)] = new()
        {
            new() { Id = G(611), FromSeller = false, Body = "Est-ce compatible avec un iPhone 14 ?", SentAt = new(2026, 6, 23, 15, 0, 0) },
            new() { Id = G(612), FromSeller = true,  Body = "Oui, parfaitement compatible.",          SentAt = new(2026, 6, 23, 16, 20, 0) },
            new() { Id = G(613), FromSeller = false, Body = "Merci pour votre réponse rapide !",       SentAt = new(2026, 6, 23, 16, 40, 0) },
        };
        _messages[G(503)] = new()
        {
            new() { Id = G(621), FromSeller = false, Body = "Pouvez-vous m'envoyer la facture ?", SentAt = new(2026, 6, 22, 11, 5, 0) },
        };

        var casqueId = _data.Products[0].Id;   // Casque audio
        var enceinteId = _data.Products[2].Id; // Enceinte Bluetooth
        _reviews = new()
        {
            new() { Id = G(701), ProductId = casqueId,   Author = "Aïssata D.", Rating = 5, Body = "Excellent son, batterie qui tient bien. Je recommande.", CreatedAt = new(2026, 6, 20) },
            new() { Id = G(702), ProductId = casqueId,   Author = "Kwame M.",   Rating = 4, Body = "Bon casque mais un peu serré au début.", CreatedAt = new(2026, 6, 18), SellerReply = "Merci pour votre retour ! Le serrage se détend après quelques jours." },
            new() { Id = G(703), ProductId = casqueId,   Author = "Anonyme",    Rating = 1, Body = "Reçu cassé, très déçu.", CreatedAt = new(2026, 6, 15), Flagged = true },
            new() { Id = G(704), ProductId = enceinteId, Author = "Fatou N.",   Rating = 5, Body = "Super enceinte, son puissant pour la taille.", CreatedAt = new(2026, 6, 19) },
        };
    }

    private static Task Done() => Task.CompletedTask;
    private static Task<T> Ok<T>(T v) => Task.FromResult(v);

    // ---------- Auth ----------
    public Task<AuthResult> LoginAsync(LoginRequest request)
        => Ok(new AuthResult("mock-access-token", "mock-refresh-token", DateTime.UtcNow.AddHours(1), _shop.ShopName));
    public Task<AuthResult> RefreshAsync(RefreshRequest request)
        => Ok(new AuthResult("mock-access-token-2", request.RefreshToken, DateTime.UtcNow.AddHours(1), _shop.ShopName));
    public Task LogoutAsync(LogoutRequest request) => Done();

    // ---------- Boutique ----------
    public Task<Shop> GetShopAsync() => Ok(_shop);
    public Task<Shop> OnboardAsync(OnboardRequest request) { _shop.ShopName = request.ShopName; return Ok(_shop); }
    public Task UpdateProfileAsync(ProfileRequest request)
    {
        _shop.ShopName = request.ShopName;
        _shop.Description = request.Description;
        _shop.LogoUrl = request.LogoUrl;
        return Done();
    }
    public Task SetPayoutAccountAsync(PayoutAccountRequest request)
    {
        _shop.Payout = new PayoutAccount { Provider = request.Provider, AccountNumber = request.AccountNumber, AccountName = request.AccountName };
        return Done();
    }
    public Task UploadKybDocumentAsync(KybDocumentRequest request)
    {
        _shop.Documents.Add(new KybDocument { Id = Guid.NewGuid(), Type = request.Type, FileUrl = request.FileUrl, Status = KybStatus.Submitted, UploadedAt = DateTime.UtcNow });
        return Done();
    }
    public Task UploadKybDocumentFileAsync(string type, string fileName, string contentType, byte[] content)
    {
        _shop.Documents.Add(new KybDocument { Id = Guid.NewGuid(), Type = type, FileUrl = fileName, Status = KybStatus.Submitted, UploadedAt = DateTime.UtcNow });
        return Done();
    }

    // ---------- Catalogue ----------
    public Task<IReadOnlyList<SellerCategory>> ListCategoriesAsync() => Ok<IReadOnlyList<SellerCategory>>(new List<SellerCategory>
    {
        new() { Id = new Guid("00000000-0000-0000-0000-000000000040"), Name = "Électronique", Slug = "electronique", Path = "electronique", Status = "Active" },
        new() { Id = new Guid("00000000-0000-0000-0000-000000000041"), ParentId = new Guid("00000000-0000-0000-0000-000000000040"), Name = "Audio", Slug = "audio", Path = "electronique/audio", Status = "Active" },
        new() { Id = new Guid("00000000-0000-0000-0000-000000000042"), Name = "Mode", Slug = "mode", Path = "mode", Status = "Active" },
    });

    public Task<IReadOnlyList<SellerBrand>> ListBrandsAsync() => Ok<IReadOnlyList<SellerBrand>>(new List<SellerBrand>
    {
        new() { Id = new Guid("00000000-0000-0000-0000-000000000140"), Name = "Sony", Slug = "sony", Status = "Active" },
        new() { Id = new Guid("00000000-0000-0000-0000-000000000141"), Name = "Nike", Slug = "nike", Status = "Active" },
    });

    public Task<CatalogProduct> GetProductAsync(Guid id)
    {
        var p = _data.Products.FirstOrDefault(x => x.Id == id);
        return Ok(new CatalogProduct { Id = id, Name = p?.Name ?? "Produit", Category = p?.Category ?? "—", Status = "active" });
    }

    public Task<CatalogProduct> CreateProductAsync(CreateProductRequest request, IReadOnlyList<ProductImageUpload> images)
    {
        var category = request.Tags?.FirstOrDefault() ?? "Divers";

        var gallery = images.Select((u, i) => new ProductImage
        {
            Url = u.DataUrl,            // aperçu réel du fichier choisi
            Emoji = "🖼️",
            AltText = u.FileName,
            IsPrimary = i == 0
        }).ToList();

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Sku = $"NEW-{Random.Shared.Next(100, 999)}",
            Name = request.Name ?? "Nouveau produit",
            Category = category,
            Emoji = gallery.Count > 0 ? "🖼️" : "📦",
            Images = gallery
        };
        _data.AddProduct(product);
        return Ok(new CatalogProduct { Id = product.Id, Name = product.Name, Description = request.Description, Category = category, Status = "draft", Tags = request.Tags ?? new() });
    }

    public Task UpdateProductAsync(Guid id, UpdateProductRequest request) => Done();
    public Task SetProductStatusAsync(Guid id, CatalogStatusRequest request) => Done();

    public Task AddMediaAsync(Guid id, MediaRequest request)
    {
        var p = _data.Products.FirstOrDefault(x => x.Id == id);
        if (p is not null)
        {
            // En démo, l'« url » porte l'emoji (placeholder visuel).
            p.Images.Add(new ProductImage
            {
                Url = request.Url,
                Emoji = string.IsNullOrWhiteSpace(request.Url) ? "🖼️" : request.Url!,
                AltText = request.AltText,
                IsPrimary = request.IsPrimary
            });
        }
        return Done();
    }

    // ---------- Offres ----------
    public Task<IReadOnlyList<Offer>> ListOffersAsync() => Ok<IReadOnlyList<Offer>>(_offers);
    public Task<Offer> GetOfferAsync(Guid id) => Ok(_offers.First(o => o.Id == id));
    public Task<Offer> CreateOfferAsync(CreateOfferRequest request)
    {
        var offer = new Offer { Id = Guid.NewGuid(), ProductId = request.ProductId, ProductName = "Nouveau produit", Sku = request.Sku, BasePriceAmount = request.BasePriceAmount, Currency = request.Currency, Condition = request.Condition, FulfillmentType = request.FulfillmentType, HandlingTime = request.HandlingTime, Status = "active" };
        _offers.Add(offer);
        return Ok(offer);
    }
    public Task SetPriceAsync(Guid id, OfferPriceRequest request)
    { var o = _offers.FirstOrDefault(x => x.Id == id); if (o is not null) o.BasePriceAmount = request.Amount; return Done(); }
    public Task SetStatusAsync(Guid id, OfferStatusRequest request)
    { var o = _offers.FirstOrDefault(x => x.Id == id); if (o is not null) o.Status = request.Status; return Done(); }
    public Task SetHandlingTimeAsync(Guid id, HandlingTimeRequest request)
    { var o = _offers.FirstOrDefault(x => x.Id == id); if (o is not null) o.HandlingTime = request.HandlingTime; return Done(); }

    // ---------- Exécution ----------
    public Task<IReadOnlyList<Shipment>> ListShipmentsAsync() => Ok<IReadOnlyList<Shipment>>(_shipments);
    public Task<Shipment> GetShipmentAsync(Guid id) => Ok(_shipments.First(s => s.Id == id));
    public Task<IReadOnlyList<Shipment>> GetByOrderAsync(Guid orderId)
        => Ok<IReadOnlyList<Shipment>>(_shipments.Where(s => s.OrderId == orderId).ToList());
    public Task PrepareAsync(Guid id)
    { var s = _shipments.FirstOrDefault(x => x.Id == id); if (s is not null) s.Status = ShipmentStatus.Prepared; return Done(); }
    public Task ShipAsync(Guid id, ShipRequest request)
    { var s = _shipments.FirstOrDefault(x => x.Id == id); if (s is not null) { s.Status = ShipmentStatus.Shipped; s.Carrier = request.Carrier; s.TrackingNumber = request.TrackingNumber; } return Done(); }
    public Task DeliverAsync(Guid id)
    { var s = _shipments.FirstOrDefault(x => x.Id == id); if (s is not null) s.Status = ShipmentStatus.Delivered; return Done(); }
    public Task CancelAsync(Guid id)
    { var s = _shipments.FirstOrDefault(x => x.Id == id); if (s is not null) s.Status = ShipmentStatus.Cancelled; return Done(); }

    // ---------- Finances ----------
    public Task<FinanceStatement> GetStatementAsync(DateTime? from = null, DateTime? to = null)
    {
        var statement = new FinanceStatement
        {
            From = from ?? new DateTime(2026, 6, 1),
            To = to ?? new DateTime(2026, 6, 25),
            GrossSalesXof = 262500,
            CommissionXof = 26250,
            RefundsXof = 24900,
            Lines =
            {
                new() { Date = new(2026, 6, 24), Label = "CMD-10428 · vente",      Type = "sale",       AmountXof = 21400 },
                new() { Date = new(2026, 6, 24), Label = "Commission plateforme",  Type = "commission", AmountXof = -2140 },
                new() { Date = new(2026, 6, 23), Label = "CMD-10426 · vente",      Type = "sale",       AmountXof = 38600 },
                new() { Date = new(2026, 6, 23), Label = "Commission plateforme",  Type = "commission", AmountXof = -3860 },
                new() { Date = new(2026, 6, 22), Label = "CMD-10423 · remboursement", Type = "refund",  AmountXof = -24900 },
                new() { Date = new(2026, 6, 20), Label = "Versement Wave",         Type = "payout",     AmountXof = -150000 },
            }
        };
        return Ok(statement);
    }

    public Task<IReadOnlyList<Payout>> ListPayoutsAsync() => Ok<IReadOnlyList<Payout>>(new List<Payout>
    {
        new() { Id = G(801), AmountXof = 150000, Status = PayoutStatus.Paid,       RequestedAt = new(2026, 6, 20), Provider = "Wave" },
        new() { Id = G(802), AmountXof = 87600,  Status = PayoutStatus.Processing, RequestedAt = new(2026, 6, 24), Provider = "Wave" },
        new() { Id = G(803), AmountXof = 42300,  Status = PayoutStatus.Pending,    RequestedAt = new(2026, 6, 25), Provider = "Wave" },
    });

    // ---------- Messagerie ----------
    public Task<IReadOnlyList<Conversation>> ListConversationsAsync() => Ok<IReadOnlyList<Conversation>>(_conversations);
    public Task<IReadOnlyList<Message>> GetMessagesAsync(Guid conversationId)
        => Ok<IReadOnlyList<Message>>(_messages.TryGetValue(conversationId, out var m) ? m : new List<Message>());
    public Task<Message> SendAsync(Guid conversationId, SendRequest request)
    {
        var msg = new Message { Id = Guid.NewGuid(), FromSeller = true, Body = request.Body, SentAt = DateTime.Now };
        if (!_messages.ContainsKey(conversationId)) _messages[conversationId] = new();
        _messages[conversationId].Add(msg);
        var c = _conversations.FirstOrDefault(x => x.Id == conversationId);
        if (c is not null) { c.LastMessage = request.Body; c.LastAt = msg.SentAt; c.Unread = 0; }
        return Ok(msg);
    }

    // ---------- Avis ----------
    public Task<IReadOnlyList<Review>> ListReviewsAsync() => Ok<IReadOnlyList<Review>>(_reviews);

    public Task<IReadOnlyList<Review>> GetProductReviewsAsync(Guid productId)
        => Ok<IReadOnlyList<Review>>(_reviews.Where(r => productId == Guid.Empty || r.ProductId == productId).ToList());
    public Task<ProductRating> GetProductRatingAsync(Guid productId)
    {
        var set = _reviews.Where(r => r.ProductId == productId).ToList();
        var breakdown = new int[5];
        foreach (var r in set) breakdown[Math.Clamp(r.Rating - 1, 0, 4)]++;
        return Ok(new ProductRating { ProductId = productId, Average = set.Count > 0 ? set.Average(r => r.Rating) : 0, Count = set.Count, Breakdown = breakdown });
    }
    public Task FlagAsync(Guid reviewId)
    { var r = _reviews.FirstOrDefault(x => x.Id == reviewId); if (r is not null) r.Flagged = true; return Done(); }
    public Task ReplyAsync(Guid reviewId, string body)
    { var r = _reviews.FirstOrDefault(x => x.Id == reviewId); if (r is not null) r.SellerReply = body; return Done(); }

    /// <summary>Identifiant du produit vedette (pour la page Avis de démo).</summary>
    public Guid FeaturedProductId => _data.Products[0].Id;
    public IReadOnlyList<Review> AllReviews => _reviews;
}
