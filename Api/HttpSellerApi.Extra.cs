using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Seller_MP_Dashboard.Models;

namespace Seller_MP_Dashboard.Api;

// Implémentation HTTP des domaines ajoutés au spec :
// Catalogue (liste/suppression/variants), Compte, Retours, Litiges, Stock.
public partial class HttpSellerApi
{
    // ---------- Catalogue (étendu) ----------
    public async Task<IReadOnlyList<Product>> ListProductsAsync()
    {
        var summaries = await _http.GetFromJsonAsync<List<ProductSummaryDto>>("/seller/products") ?? new();
        return summaries.Select(MapProduct).ToList();
    }

    /// <summary>Détail d'un produit (avec galerie d'images) pour la page détail.</summary>
    public async Task<Product?> GetProductDetailAsync(Guid id)
    {
        var dto = await _http.GetFromJsonAsync<ProductSummaryDto>($"/seller/products/{id}");
        return dto is null ? null : MapProduct(dto);
    }

    private static Product MapProduct(ProductSummaryDto s) => new()
    {
        Id = s.Id,
        Name = s.Name,
        CategoryId = s.CategoryId,
        Description = s.Description,
        Sku = s.Variants.FirstOrDefault()?.Sku ?? "",
        Status = s.Status,
        Gtin = s.Gtin,
        Ean = s.Ean,
        ProductGroupId = s.ProductGroupId,
        Attributes = s.Attributes ?? new(),
        Tags = s.Tags?.ToList() ?? new(),
        Images = (s.Media ?? new())
            .OrderByDescending(m => m.IsPrimary)
            .ThenBy(m => m.Position)
            .Select(m => new ProductImage { Id = m.Id, Url = m.Url, AltText = m.AltText, IsPrimary = m.IsPrimary })
            .ToList(),
        Variants = (s.Variants ?? new())
            .Select(v => new ProductVariant
            {
                Id = v.Id,
                Sku = v.Sku,
                Barcode = v.Barcode,
                WeightGrams = v.WeightGrams,
                Attributes = v.Attributes ?? new()
            })
            .ToList()
    };

    // DTO permissif aligné sur ProductSummary du BFF (désérialisation tolérante).
    private sealed class ProductSummaryDto
    {
        public Guid Id { get; set; }
        public Guid CategoryId { get; set; }
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string Status { get; set; } = "";
        public string? Gtin { get; set; }
        public string? Ean { get; set; }
        public Guid? ProductGroupId { get; set; }
        public Dictionary<string, string>? Attributes { get; set; }
        public List<string>? Tags { get; set; }
        public List<VariantDto> Variants { get; set; } = new();
        public List<MediaDto>? Media { get; set; }
    }

    private sealed class VariantDto
    {
        public Guid Id { get; set; }
        public string Sku { get; set; } = "";
        public string? Barcode { get; set; }
        public int WeightGrams { get; set; }
        public Dictionary<string, string>? Attributes { get; set; }
    }

    private sealed class MediaDto
    {
        public Guid Id { get; set; }
        public string? Url { get; set; }
        public bool IsPrimary { get; set; }
        public int Position { get; set; }
        public string? AltText { get; set; }
    }

    public Task DeleteProductAsync(Guid id)
        => _http.DeleteAsync($"/seller/products/{id}");

    // ---------- Images d'un produit existant ----------
    public async Task UploadImageAsync(Guid id, ProductImageUpload image)
    {
        using var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(image.Content);
        file.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(
            string.IsNullOrEmpty(image.ContentType) ? "image/jpeg" : image.ContentType);
        form.Add(file, "file", image.FileName);
        if (!string.IsNullOrWhiteSpace(image.AltText))
        {
            form.Add(new StringContent(image.AltText), "alt");
        }
        var resp = await _http.PostAsync($"/seller/products/{id}/media/upload", form);
        resp.EnsureSuccessStatusCode();
    }

    public Task SetPrimaryImageAsync(Guid id, Guid mediaId)
        => _http.PostAsync($"/seller/products/{id}/media/{mediaId}/primary", null);

    public Task ReorderImagesAsync(Guid id, IReadOnlyList<Guid> orderedMediaIds)
        => _http.PutAsJsonAsync($"/seller/products/{id}/media/order", new { orderedMediaIds });

    public Task RemoveImageAsync(Guid id, Guid mediaId)
        => _http.DeleteAsync($"/seller/products/{id}/media/{mediaId}");

    public Task AddVariantAsync(Guid id, VariantRequest request)
        => _http.PostAsJsonAsync($"/seller/products/{id}/variants", request);

    public Task UpdateVariantAsync(Guid id, Guid variantId, VariantRequest request)
        => _http.PutAsJsonAsync($"/seller/products/{id}/variants/{variantId}", request);

    public Task RemoveVariantAsync(Guid id, Guid variantId)
        => _http.DeleteAsync($"/seller/products/{id}/variants/{variantId}");

    // ---------- Compte ----------
    public async Task<AccountMe> GetMeAsync()
        => (await _http.GetFromJsonAsync<AccountMe>("/seller/me"))!;

    public async Task<AccountMe> GetAccountAsync()
        => (await _http.GetFromJsonAsync<AccountMe>("/seller/account/me"))!;

    public Task UpdateAccountAsync(AccountProfileRequest request)
        => _http.PutAsJsonAsync("/seller/account/me", request);

    public Task ChangePasswordAsync(ChangePasswordRequest request)
        => _http.PostAsJsonAsync("/seller/account/me/change-password", request);

    public async Task<MfaSetupResult> MfaSetupAsync()
    {
        var resp = await _http.PostAsync("/seller/account/me/mfa/setup", null);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<MfaSetupResult>())!;
    }

    public Task MfaConfirmAsync(MfaCodeRequest request)
        => _http.PostAsJsonAsync("/seller/account/me/mfa/confirm", request);

    public Task MfaDisableAsync(MfaCodeRequest request)
        => _http.PostAsJsonAsync("/seller/account/me/mfa/disable", request);

    // ---------- Retours ----------
    public async Task<IReadOnlyList<Return>> ListReturnsAsync()
        => (await _http.GetFromJsonAsync<List<Return>>("/seller/returns")) ?? new();

    public async Task<Return> GetReturnAsync(Guid id)
        => (await _http.GetFromJsonAsync<Return>($"/seller/returns/{id}"))!;

    public Task ApproveReturnAsync(Guid id)
        => _http.PostAsync($"/seller/returns/{id}/approve", null);

    public Task RejectReturnAsync(Guid id, ReturnRejectRequest request)
        => _http.PostAsJsonAsync($"/seller/returns/{id}/reject", request);

    public Task SetReturnTrackingAsync(Guid id, ReturnTrackingRequest request)
        => _http.PostAsJsonAsync($"/seller/returns/{id}/tracking", request);

    public Task MarkReturnReceivedAsync(Guid id)
        => _http.PostAsync($"/seller/returns/{id}/received", null);

    public Task RefundReturnAsync(Guid id, ReturnRefundRequest request)
        => _http.PostAsJsonAsync($"/seller/returns/{id}/refund", request);

    // ---------- Litiges ----------
    public async Task<IReadOnlyList<Dispute>> GetDisputesByOrderAsync(Guid orderId)
        => (await _http.GetFromJsonAsync<List<Dispute>>($"/seller/disputes/by-order/{orderId}")) ?? new();

    public async Task<Dispute> GetDisputeAsync(Guid id)
        => (await _http.GetFromJsonAsync<Dispute>($"/seller/disputes/{id}"))!;

    public Task AddDisputeMessageAsync(Guid id, DisputeMessageRequest request)
        => _http.PostAsJsonAsync($"/seller/disputes/{id}/messages", request);

    // ---------- Stock & lieux ----------
    public async Task<IReadOnlyList<Location>> ListLocationsAsync()
        => (await _http.GetFromJsonAsync<List<Location>>("/seller/locations")) ?? new();

    public async Task<Location> CreateLocationAsync(LocationRequest request)
        => await PostJson<Location>("/seller/locations", request);

    public async Task<IReadOnlyList<InventoryItem>> ListInventoryBySkuAsync(string sku)
        => (await _http.GetFromJsonAsync<List<InventoryItem>>($"/seller/inventory/by-sku/{Uri.EscapeDataString(sku)}")) ?? new();

    public async Task<InventoryItem> CreateItemAsync(CreateInventoryItemRequest request)
        => await PostJson<InventoryItem>("/seller/inventory/items", request);

    public Task ReceiveAsync(Guid itemId, QuantityRequest request)
        => _http.PostAsJsonAsync($"/seller/inventory/items/{itemId}/receive", request);

    public Task AdjustAsync(Guid itemId, DeltaRequest request)
        => _http.PostAsJsonAsync($"/seller/inventory/items/{itemId}/adjust", request);

    public Task SetReorderThresholdAsync(Guid itemId, ThresholdRequest request)
        => _http.PutAsJsonAsync($"/seller/inventory/items/{itemId}/reorder-threshold", request);

    public async Task<InventoryAvailability> GetAvailabilityAsync(string sku)
        => (await _http.GetFromJsonAsync<InventoryAvailability>($"/seller/inventory/availability/{sku}"))!;
}
