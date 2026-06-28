namespace Seller_MP_Dashboard.Api;

// ============================================================
// Surface API du BFF Vendeur, découpée par domaine (= tags BFF).
// Une seule abstraction utilisée par l'UI ; deux implémentations :
//   - MockSellerApi  (données fictives, enregistrée par défaut)
//   - HttpSellerApi  (appels HTTP réels, non câblée pour l'instant)
// ============================================================

public interface ISellerAuthApi
{
    Task<AuthResult> LoginAsync(LoginRequest request);
    Task<AuthResult> RefreshAsync(RefreshRequest request);
    Task LogoutAsync(LogoutRequest request);
}

public interface ISellerShopApi
{
    Task<Shop> GetShopAsync();
    Task<Shop> OnboardAsync(OnboardRequest request);
    Task UpdateProfileAsync(ProfileRequest request);
    Task SetPayoutAccountAsync(PayoutAccountRequest request);
    Task UploadKybDocumentAsync(KybDocumentRequest request);
    Task UploadKybDocumentFileAsync(string type, string fileName, string contentType, byte[] content);
}

public interface ISellerCatalogApi
{
    Task<IReadOnlyList<Seller_MP_Dashboard.Models.Product>> ListProductsAsync();
    Task<Seller_MP_Dashboard.Models.Product?> GetProductDetailAsync(Guid id);
    Task<IReadOnlyList<SellerCategory>> ListCategoriesAsync();
    Task<IReadOnlyList<SellerBrand>> ListBrandsAsync();
    Task<CatalogProduct> GetProductAsync(Guid id);
    Task<CatalogProduct> CreateProductAsync(CreateProductRequest request, IReadOnlyList<Seller_MP_Dashboard.Models.ProductImageUpload> images);
    Task UpdateProductAsync(Guid id, UpdateProductRequest request);
    Task DeleteProductAsync(Guid id);
    Task SetProductStatusAsync(Guid id, CatalogStatusRequest request);
    Task AddMediaAsync(Guid id, MediaRequest request);
    Task AddVariantAsync(Guid id, VariantRequest request);
    Task UpdateVariantAsync(Guid id, Guid variantId, VariantRequest request);
    Task RemoveVariantAsync(Guid id, Guid variantId);

    // Gestion des images d'un produit existant.
    Task UploadImageAsync(Guid id, Seller_MP_Dashboard.Models.ProductImageUpload image);
    Task SetPrimaryImageAsync(Guid id, Guid mediaId);
    Task ReorderImagesAsync(Guid id, IReadOnlyList<Guid> orderedMediaIds);
    Task RemoveImageAsync(Guid id, Guid mediaId);
}

public interface ISellerAccountApi
{
    Task<AccountMe> GetMeAsync();
    Task<AccountMe> GetAccountAsync();
    Task UpdateAccountAsync(AccountProfileRequest request);
    Task ChangePasswordAsync(ChangePasswordRequest request);
    Task<MfaSetupResult> MfaSetupAsync();
    Task MfaConfirmAsync(MfaCodeRequest request);
    Task MfaDisableAsync(MfaCodeRequest request);
}

public interface ISellerReturnsApi
{
    Task<IReadOnlyList<Return>> ListReturnsAsync();
    Task<Return> GetReturnAsync(Guid id);
    Task ApproveReturnAsync(Guid id);
    Task RejectReturnAsync(Guid id, ReturnRejectRequest request);
    Task SetReturnTrackingAsync(Guid id, ReturnTrackingRequest request);
    Task MarkReturnReceivedAsync(Guid id);
    Task RefundReturnAsync(Guid id, ReturnRefundRequest request);
}

public interface ISellerDisputesApi
{
    Task<IReadOnlyList<Dispute>> GetDisputesByOrderAsync(Guid orderId);
    Task<Dispute> GetDisputeAsync(Guid id);
    Task AddDisputeMessageAsync(Guid id, DisputeMessageRequest request);
}

public interface ISellerInventoryApi
{
    Task<IReadOnlyList<Location>> ListLocationsAsync();
    Task<Location> CreateLocationAsync(LocationRequest request);
    Task<IReadOnlyList<InventoryItem>> ListInventoryBySkuAsync(string sku);
    Task<InventoryItem> CreateItemAsync(CreateInventoryItemRequest request);
    Task ReceiveAsync(Guid itemId, QuantityRequest request);
    Task AdjustAsync(Guid itemId, DeltaRequest request);
    Task SetReorderThresholdAsync(Guid itemId, ThresholdRequest request);
    Task<InventoryAvailability> GetAvailabilityAsync(string sku);
}

public interface ISellerOffersApi
{
    Task<IReadOnlyList<Offer>> ListOffersAsync();
    Task<Offer> GetOfferAsync(Guid id);
    Task<Offer> CreateOfferAsync(CreateOfferRequest request);
    Task SetPriceAsync(Guid id, OfferPriceRequest request);
    Task SetStatusAsync(Guid id, OfferStatusRequest request);
    Task SetHandlingTimeAsync(Guid id, HandlingTimeRequest request);
}

public interface ISellerFulfillmentApi
{
    Task<IReadOnlyList<Shipment>> ListShipmentsAsync();
    Task<Shipment> GetShipmentAsync(Guid id);
    Task<IReadOnlyList<Shipment>> GetByOrderAsync(Guid orderId);
    Task PrepareAsync(Guid id);
    Task ShipAsync(Guid id, ShipRequest request);
    Task DeliverAsync(Guid id);
    Task CancelAsync(Guid id);
}

public interface ISellerOrdersApi
{
    Task<IReadOnlyList<SellerOrder>> ListOrdersAsync();
    Task<SellerOrder?> GetOrderAsync(Guid id);
}

public interface ISellerDashboardApi
{
    Task<SellerDashboardKpis> GetDashboardAsync();
}

public interface ISellerFinanceApi
{
    Task<FinanceStatement> GetStatementAsync(DateTime? from = null, DateTime? to = null);
    Task<IReadOnlyList<Payout>> ListPayoutsAsync();
}

public interface ISellerMessagingApi
{
    Task<IReadOnlyList<Conversation>> ListConversationsAsync();
    Task<IReadOnlyList<Message>> GetMessagesAsync(Guid conversationId);
    Task<Message> SendAsync(Guid conversationId, SendRequest request);
}

public interface ISellerReviewsApi
{
    Task<IReadOnlyList<Review>> ListReviewsAsync();
    Task<IReadOnlyList<Review>> GetProductReviewsAsync(Guid productId);
    Task<ProductRating> GetProductRatingAsync(Guid productId);
    Task FlagAsync(Guid reviewId);
    Task ReplyAsync(Guid reviewId, string body);
}

/// <summary>Agrégat pratique de tous les domaines du BFF Vendeur.</summary>
public interface ISellerApi :
    ISellerAuthApi, ISellerShopApi, ISellerCatalogApi, ISellerAccountApi, ISellerOffersApi,
    ISellerOrdersApi, ISellerDashboardApi, ISellerFulfillmentApi, ISellerFinanceApi, ISellerMessagingApi,
    ISellerReviewsApi, ISellerReturnsApi, ISellerDisputesApi, ISellerInventoryApi
{
}
