using Seller_MP_Dashboard.Models;

namespace Seller_MP_Dashboard.Api;

// Mock des domaines ajoutés : Catalogue (liste/suppression/variants),
// Compte, Retours, Litiges, Stock.
public partial class MockSellerApi
{
    // ---------- Catalogue (étendu) ----------
    public Task<IReadOnlyList<Product>> ListProductsAsync()
        => Ok<IReadOnlyList<Product>>(_data.Products);

    public Task<Product?> GetProductDetailAsync(Guid id)
        => Task.FromResult(_data.Products.FirstOrDefault(p => p.Id == id));

    public Task DeleteProductAsync(Guid id) { _data.RemoveProduct(id); return Done(); }

    public Task UploadImageAsync(Guid id, ProductImageUpload image) => Done();
    public Task SetPrimaryImageAsync(Guid id, Guid mediaId) => Done();
    public Task ReorderImagesAsync(Guid id, IReadOnlyList<Guid> orderedMediaIds) => Done();
    public Task RemoveImageAsync(Guid id, Guid mediaId) => Done();

    public Task AddVariantAsync(Guid id, VariantRequest request) => Done();
    public Task UpdateVariantAsync(Guid id, Guid variantId, VariantRequest request) => Done();
    public Task RemoveVariantAsync(Guid id, Guid variantId) => Done();

    // ---------- Commandes ----------
    private List<SellerOrder> BuildOrders() => _data.Orders.Select((o, i) => new SellerOrder
    {
        Id = G(900 + i),
        BuyerId = G(800 + i),
        Status = o.Status.ToString().ToLowerInvariant(),
        CreatedAtUtc = o.Date,
        Subtotal = o.TotalXof,
        GrandTotal = o.TotalXof,
        Lines = o.Items.Select(l => new SellerOrderLine
        {
            Sku = l.ProductName,
            Quantity = l.Qty,
            FinalUnitPrice = l.UnitPriceXof,
            LineTotal = l.LineTotalXof
        }).ToList()
    }).ToList();

    public Task<IReadOnlyList<SellerOrder>> ListOrdersAsync() => Ok<IReadOnlyList<SellerOrder>>(BuildOrders());

    public Task<SellerOrder?> GetOrderAsync(Guid id) => Task.FromResult(BuildOrders().FirstOrDefault(o => o.Id == id));

    // ---------- Tableau de bord ----------
    public Task<SellerDashboardKpis> GetDashboardAsync()
    {
        var orders = _data.Orders;
        return Ok(new SellerDashboardKpis
        {
            OrdersTotal = orders.Count,
            OrdersToProcess = orders.Count(o => o.Status is OrderStatus.Pending or OrderStatus.Processing),
            GrossSales30d = orders.Where(o => o.Payment == PaymentStatus.Paid).Sum(o => o.TotalXof),
            NetPayout30d = (decimal)(orders.Where(o => o.Payment == PaymentStatus.Paid).Sum(o => o.TotalXof) * 0.9),
            Currency = "XOF",
            ReviewsCount = 0,
            AverageRating = 0
        });
    }

    // ---------- Compte ----------
    private AccountMe BuildAccount()
    {
        var u = _data.CurrentUser;
        var parts = u.FullName.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        return new AccountMe
        {
            Id = G(2),
            FirstName = parts.Length > 0 ? parts[0] : "",
            LastName = parts.Length > 1 ? parts[1] : "",
            Email = u.Email,
            PhoneNumber = u.Phone,
            Role = u.Role,
            ShopName = u.ShopName,
            MfaEnabled = u.MfaEnabled,
            CreatedAt = u.MemberSince
        };
    }

    private AccountMe _account = null!;
    private AccountMe Account => _account ??= BuildAccount();

    public Task<AccountMe> GetMeAsync() => Ok(Account);
    public Task<AccountMe> GetAccountAsync() => Ok(Account);

    public Task UpdateAccountAsync(AccountProfileRequest request)
    {
        Account.FirstName = request.FirstName;
        Account.LastName = request.LastName;
        Account.PhoneNumber = request.PhoneNumber;
        return Done();
    }

    public Task ChangePasswordAsync(ChangePasswordRequest request) => Done();
    public Task<MfaSetupResult> MfaSetupAsync() => Ok(new MfaSetupResult { SecretKey = "MOCK-SECRET-1234", QrCodeUri = null });
    public Task MfaConfirmAsync(MfaCodeRequest request) { Account.MfaEnabled = true; return Done(); }
    public Task MfaDisableAsync(MfaCodeRequest request) { Account.MfaEnabled = false; return Done(); }

    // ---------- Retours ----------
    private readonly List<Return> _returns = new()
    {
        new() { Id = G(901), OrderId = G(403), OrderReference = "CMD-10426", Customer = "Fatou Ndiaye", Reason = "Produit non conforme", Status = ReturnStatus.Requested, AmountXof = 6500,  RequestedAt = new(2026, 6, 24) },
        new() { Id = G(902), OrderId = G(404), OrderReference = "CMD-10425", Customer = "Ibrahim Touré", Reason = "Changé d'avis",        Status = ReturnStatus.Approved,  AmountXof = 8900,  RequestedAt = new(2026, 6, 23) },
        new() { Id = G(903), OrderId = G(401), OrderReference = "CMD-10428", Customer = "Aïssata Diallo",Reason = "Article endommagé",    Status = ReturnStatus.Refunded,  AmountXof = 12500, RequestedAt = new(2026, 6, 20) },
    };

    public Task<IReadOnlyList<Return>> ListReturnsAsync() => Ok<IReadOnlyList<Return>>(_returns);
    public Task<Return> GetReturnAsync(Guid id) => Ok(_returns.First(r => r.Id == id));
    public Task ApproveReturnAsync(Guid id) { Set(id, ReturnStatus.Approved); return Done(); }
    public Task RejectReturnAsync(Guid id, ReturnRejectRequest request) { Set(id, ReturnStatus.Rejected); return Done(); }
    public Task SetReturnTrackingAsync(Guid id, ReturnTrackingRequest request)
    { var r = _returns.FirstOrDefault(x => x.Id == id); if (r is not null) { r.Status = ReturnStatus.InTransit; r.Carrier = request.Carrier; r.TrackingNumber = request.TrackingNumber; } return Done(); }
    public Task MarkReturnReceivedAsync(Guid id) { Set(id, ReturnStatus.Received); return Done(); }
    public Task RefundReturnAsync(Guid id, ReturnRefundRequest request) { Set(id, ReturnStatus.Refunded); return Done(); }
    private void Set(Guid id, ReturnStatus s) { var r = _returns.FirstOrDefault(x => x.Id == id); if (r is not null) r.Status = s; }

    // ---------- Litiges ----------
    private readonly List<Dispute> _disputes = new()
    {
        new()
        {
            Id = G(1001), OrderId = G(403), OrderReference = "CMD-10426", Customer = "Fatou Ndiaye",
            Subject = "Colis non reçu", Status = DisputeStatus.AwaitingSeller, OpenedAt = new(2026, 6, 24),
            Messages = new()
            {
                new() { Id = G(1011), FromSeller = false, Body = "Je n'ai pas reçu mon colis.", SentAt = new(2026, 6, 24, 10, 0, 0) },
            }
        }
    };

    public Task<IReadOnlyList<Dispute>> GetDisputesByOrderAsync(Guid orderId)
        => Ok<IReadOnlyList<Dispute>>(_disputes.Where(d => d.OrderId == orderId).ToList());
    public Task<Dispute> GetDisputeAsync(Guid id) => Ok(_disputes.First(d => d.Id == id));
    public Task AddDisputeMessageAsync(Guid id, DisputeMessageRequest request)
    {
        var d = _disputes.FirstOrDefault(x => x.Id == id);
        d?.Messages.Add(new DisputeMessage { Id = Guid.NewGuid(), FromSeller = true, Body = request.Body, PhotoUrl = request.PhotoUrl, SentAt = DateTime.Now });
        return Done();
    }

    // ---------- Stock & lieux ----------
    private readonly List<InventoryItem> _items = new();

    public Task<IReadOnlyList<Location>> ListLocationsAsync() => Ok<IReadOnlyList<Location>>(new List<Location>
    {
        new() { Id = new Guid("00000000-0000-0000-0000-000000000200"), Line = "12 rue du Commerce", City = "Dakar", Country = "SN" },
    });

    public Task<Location> CreateLocationAsync(LocationRequest request)
        => Ok(new Location { Id = Guid.NewGuid(), Line = request.Line, City = request.City, Country = request.Country });

    public Task<IReadOnlyList<InventoryItem>> ListInventoryBySkuAsync(string sku)
        => Ok<IReadOnlyList<InventoryItem>>(new List<InventoryItem>
        {
            new() { Id = Guid.NewGuid(), Sku = sku, LocationId = new Guid("00000000-0000-0000-0000-000000000200"), OnHand = 12, Reserved = 2, ReorderThreshold = 5, IsLowStock = false },
        });

    public Task<InventoryItem> CreateItemAsync(CreateInventoryItemRequest request)
    {
        var item = new InventoryItem { Id = Guid.NewGuid(), Sku = request.Sku, LocationId = request.LocationId, OnHand = request.OnHand, ReorderThreshold = request.ReorderThreshold };
        _items.Add(item);
        return Ok(item);
    }

    public Task ReceiveAsync(Guid itemId, QuantityRequest request)
    { var i = _items.FirstOrDefault(x => x.Id == itemId); if (i is not null) i.OnHand += request.Quantity; return Done(); }
    public Task AdjustAsync(Guid itemId, DeltaRequest request)
    { var i = _items.FirstOrDefault(x => x.Id == itemId); if (i is not null) i.OnHand += request.Delta; return Done(); }
    public Task SetReorderThresholdAsync(Guid itemId, ThresholdRequest request)
    { var i = _items.FirstOrDefault(x => x.Id == itemId); if (i is not null) i.ReorderThreshold = request.Threshold; return Done(); }

    public Task<InventoryAvailability> GetAvailabilityAsync(string sku)
    {
        var item = _items.FirstOrDefault(x => string.Equals(x.Sku, sku, StringComparison.OrdinalIgnoreCase));
        var p = _data.Products.FirstOrDefault(x => string.Equals(x.Sku, sku, StringComparison.OrdinalIgnoreCase));
        var onHand = item?.OnHand ?? p?.Stock ?? 0;
        return Ok(new InventoryAvailability { Sku = sku, OnHand = onHand, Reserved = 0, Available = onHand });
    }
}
