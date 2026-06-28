using Seller_MP_Dashboard.Models;

namespace Seller_MP_Dashboard.Services;

/// <summary>
/// Fournit des données fictives mais cohérentes pour la démo du dashboard vendeur.
/// À remplacer par un appel API réel le moment venu (mêmes modèles).
/// </summary>
public class MockDataService
{
    public string SellerName => "AfriTech Store";
    public bool SellerVerified => true;
    public double OnTimeDeliveryRate => 0.98;

    /// <summary>Profil de l'utilisateur connecté (démo).</summary>
    public UserProfile CurrentUser { get; } = new()
    {
        FullName = "Awa Diop",
        Email = "awa.diop@afritech.store",
        Phone = "+221 77 123 45 67",
        Role = "Vendeur · Gérante",
        ShopName = "AfriTech Store",
        MemberSince = new DateTime(2025, 11, 3),
        MfaEnabled = true,
        Language = "Français"
    };

    private readonly List<Product> _products = new()
    {
        new() { Id = PId(1), Sku = "CAS-001", Name = "Casque audio sans fil",      Category = "Audio",       Emoji = "🎧", PriceXof = 12500, OldPriceXof = 15000, Stock = 42, Rating = 4.6, ReviewCount = 128, UnitsSold = 340,
                Images = Gallery(("🎧", "Casque vu de face"), ("📦", "Emballage"), ("🔌", "Câble & accessoires"), ("🎚️", "Commandes")) },
        new() { Id = PId(2), Sku = "MTR-014", Name = "Montre connectée sport",     Category = "Wearables",   Emoji = "⌚", PriceXof = 24900, OldPriceXof = 29900, Stock = 7,  Rating = 4.3, ReviewCount = 86,  UnitsSold = 210,
                Images = Gallery(("⌚", "Cadran"), ("📦", "Emballage"), ("📱", "Application mobile")) },
        new() { Id = PId(3), Sku = "ENC-007", Name = "Enceinte Bluetooth portable", Category = "Audio",      Emoji = "🔊", PriceXof = 8900,  OldPriceXof = null,  Stock = 64, Rating = 4.7, ReviewCount = 203, UnitsSold = 512,
                Images = Gallery(("🔊", "Enceinte"), ("📦", "Emballage"), ("🎶", "En usage")) },
        new() { Id = PId(4), Sku = "CHG-022", Name = "Chargeur rapide 65W",        Category = "Accessoires", Emoji = "🔌", PriceXof = 6500,  OldPriceXof = 7900,  Stock = 0,  Rating = 4.1, ReviewCount = 54,  UnitsSold = 175,
                Images = Gallery(("🔌", "Chargeur"), ("⚡", "Charge rapide")) },
        new() { Id = PId(5), Sku = "POW-009", Name = "Batterie externe 20 000 mAh", Category = "Accessoires", Emoji = "🔋", PriceXof = 11200, OldPriceXof = null,  Stock = 23, Rating = 4.5, ReviewCount = 97,  UnitsSold = 264,
                Images = Gallery(("🔋", "Batterie"), ("📦", "Emballage"), ("⚡", "Ports de charge")) },
        new() { Id = PId(6), Sku = "ECR-031", Name = "Écouteurs intra-auriculaires", Category = "Audio",     Emoji = "🎵", PriceXof = 4900,  OldPriceXof = 6900,  Stock = 3,  Rating = 4.2, ReviewCount = 142, UnitsSold = 398,
                Images = Gallery(("🎵", "Écouteurs"), ("📦", "Boîtier de charge"), ("👂", "Embouts")) },
        new() { Id = PId(7), Sku = "CLV-018", Name = "Clavier mécanique compact",   Category = "Informatique", Emoji = "⌨️", PriceXof = 18500, OldPriceXof = null, Stock = 31, Rating = 4.8, ReviewCount = 71,  UnitsSold = 119,
                Images = Gallery(("⌨️", "Clavier"), ("📦", "Emballage"), ("💡", "Rétroéclairage")) },
        new() { Id = PId(8), Sku = "SOU-005", Name = "Souris ergonomique sans fil",  Category = "Informatique", Emoji = "🖱️", PriceXof = 7200, OldPriceXof = 8500, Stock = 0,  Rating = 4.0, ReviewCount = 38,  UnitsSold = 142,
                Images = Gallery(("🖱️", "Souris"), ("📦", "Emballage")) },
    };

    /// <summary>Catalogue (modifiable : la création de produit y ajoute une entrée).</summary>
    public IReadOnlyList<Product> Products => _products;

    /// <summary>Ajoute un produit en tête de catalogue (démo création).</summary>
    public void AddProduct(Product product) => _products.Insert(0, product);

    /// <summary>Retire un produit du catalogue (démo suppression).</summary>
    public void RemoveProduct(Guid id) => _products.RemoveAll(p => p.Id == id);

    /// <summary>Identifiant déterministe de produit mock.</summary>
    private static Guid PId(int n) => new($"00000000-0000-0000-00aa-{n:000000000000}");

    /// <summary>Construit une galerie ; la première image est l'image principale.</summary>
    private static List<ProductImage> Gallery(params (string emoji, string alt)[] images)
    {
        var list = new List<ProductImage>();
        for (int i = 0; i < images.Length; i++)
            list.Add(new ProductImage { Emoji = images[i].emoji, AltText = images[i].alt, IsPrimary = i == 0 });
        return list;
    }

    public IReadOnlyList<Order> Orders { get; } = new List<Order>
    {
        new() { Reference = "CMD-10428", Customer = "Aïssata Diallo",   Date = new(2026, 6, 24), TotalXof = 21400, Status = OrderStatus.Pending,    Payment = PaymentStatus.Paid,     ShippingCity = "Dakar",     ShippingAddress = "Sicap Liberté 6, Villa 234",
                Items = Lines(("Casque audio sans fil", "🎧", 1, 12500), ("Enceinte Bluetooth portable", "🔊", 1, 8900)) },
        new() { Reference = "CMD-10427", Customer = "Kwame Mensah",     Date = new(2026, 6, 24), TotalXof = 12500, Status = OrderStatus.Processing, Payment = PaymentStatus.Paid,     ShippingCity = "Accra",     ShippingAddress = "Osu, 12 Oxford St",
                Items = Lines(("Casque audio sans fil", "🎧", 1, 12500)) },
        new() { Reference = "CMD-10426", Customer = "Fatou Ndiaye",     Date = new(2026, 6, 23), TotalXof = 38600, Status = OrderStatus.Shipped,    Payment = PaymentStatus.Paid,     ShippingCity = "Abidjan",   ShippingAddress = "Cocody, Rue des Jardins",
                Items = Lines(("Montre connectée sport", "⌚", 1, 24900), ("Chargeur rapide 65W", "🔌", 1, 6500), ("Souris ergonomique sans fil", "🖱️", 1, 7200)) },
        new() { Reference = "CMD-10425", Customer = "Ibrahim Touré",    Date = new(2026, 6, 23), TotalXof = 8900,  Status = OrderStatus.Delivered,  Payment = PaymentStatus.Paid,     ShippingCity = "Bamako",    ShippingAddress = "Hamdallaye ACI 2000",
                Items = Lines(("Enceinte Bluetooth portable", "🔊", 1, 8900)) },
        new() { Reference = "CMD-10424", Customer = "Mariam Coulibaly", Date = new(2026, 6, 22), TotalXof = 16100, Status = OrderStatus.Delivered,  Payment = PaymentStatus.Paid,     ShippingCity = "Ouagadougou", ShippingAddress = "Secteur 15, Avenue Kwame Nkrumah",
                Items = Lines(("Batterie externe 20 000 mAh", "🔋", 1, 11200), ("Écouteurs intra-auriculaires", "🎵", 1, 4900)) },
        new() { Reference = "CMD-10423", Customer = "Yao Konan",        Date = new(2026, 6, 22), TotalXof = 24900, Status = OrderStatus.Cancelled,  Payment = PaymentStatus.Refunded, ShippingCity = "Lomé",      ShippingAddress = "Bè, Rue 145",
                Items = Lines(("Montre connectée sport", "⌚", 1, 24900)) },
        new() { Reference = "CMD-10422", Customer = "Awa Sow",          Date = new(2026, 6, 21), TotalXof = 42800, Status = OrderStatus.Delivered,  Payment = PaymentStatus.Paid,     ShippingCity = "Dakar",     ShippingAddress = "Plateau, Rue Carnot",
                Items = Lines(("Enceinte Bluetooth portable", "🔊", 2, 8900), ("Casque audio sans fil", "🎧", 2, 12500)) },
        new() { Reference = "CMD-10421", Customer = "Cheikh Fall",      Date = new(2026, 6, 21), TotalXof = 6500,  Status = OrderStatus.Pending,    Payment = PaymentStatus.Pending,  ShippingCity = "Thiès",     ShippingAddress = "Quartier Randoulène",
                Items = Lines(("Chargeur rapide 65W", "🔌", 1, 6500)) },
    };

    private static List<OrderLine> Lines(params (string name, string emoji, int qty, int price)[] items)
        => items.Select(i => new OrderLine { ProductName = i.name, Emoji = i.emoji, Qty = i.qty, UnitPriceXof = i.price }).ToList();

    public Order? GetOrder(string reference) =>
        Orders.FirstOrDefault(o => string.Equals(o.Reference, reference, StringComparison.OrdinalIgnoreCase));

    /// <summary>Ventes des 7 derniers jours (pour le graphique de la vue d'ensemble).</summary>
    public IReadOnlyList<SalesPoint> WeeklySales { get; } = new List<SalesPoint>
    {
        new() { Label = "Lun", AmountXof = 28400 },
        new() { Label = "Mar", AmountXof = 31200 },
        new() { Label = "Mer", AmountXof = 24900 },
        new() { Label = "Jeu", AmountXof = 38600 },
        new() { Label = "Ven", AmountXof = 45100 },
        new() { Label = "Sam", AmountXof = 52300 },
        new() { Label = "Dim", AmountXof = 41800 },
    };

    public Product? GetProduct(string sku) =>
        Products.FirstOrDefault(p => string.Equals(p.Sku, sku, StringComparison.OrdinalIgnoreCase));

    public IReadOnlyList<KpiStat> GetKpis()
    {
        var revenue = Orders.Where(o => o.Payment == PaymentStatus.Paid).Sum(o => o.TotalXof);
        var pending = Orders.Count(o => o.Status is OrderStatus.Pending or OrderStatus.Processing);
        var lowStock = Products.Count(p => p.StockStatus != StockStatus.InStock);

        return new List<KpiStat>
        {
            new() { Label = "Revenus (30 j)",       Value = Format.Xof(revenue),       Sublabel = "commandes payées", TrendPercent = 12.4, Icon = "💰" },
            new() { Label = "Commandes",            Value = Orders.Count.ToString(),   Sublabel = $"{pending} à traiter", TrendPercent = 8.1, Icon = "🧾" },
            new() { Label = "Livraison à temps",    Value = $"{OnTimeDeliveryRate:P0}", Sublabel = "30 derniers jours", TrendPercent = 1.5, Icon = "🚚" },
            new() { Label = "Produits à réapprovisionner", Value = lowStock.ToString(), Sublabel = "stock faible ou rupture", TrendPercent = -3.0, Icon = "📦" },
        };
    }
}
