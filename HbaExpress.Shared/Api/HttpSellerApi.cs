using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Seller_MP_Dashboard.Models;

namespace Seller_MP_Dashboard.Api;

/// <summary>
/// Implémentation HTTP du BFF Vendeur (seule implémentation de <see cref="ISellerApi"/>).
/// Chaque méthode mappe une route de l'OpenAPI. Le HttpClient est configuré dans
/// Program.cs (BaseAddress = Api:BaseUrl + BearerAuthHandler pour le jeton).
/// </summary>
public partial class HttpSellerApi : ISellerApi
{
    private readonly HttpClient _http;
    public HttpSellerApi(HttpClient http) => _http = http;

    // ---------- Auth ----------
    public async Task<AuthResult> LoginAsync(LoginRequest request)
    {
        var resp = await _http.PostAsJsonAsync("/seller/auth/login", request);
        resp.EnsureSuccessStatusCode();
        return ParseAuth(await resp.Content.ReadAsStringAsync());
    }

    public async Task<AuthResult> RefreshAsync(RefreshRequest request)
    {
        var resp = await _http.PostAsJsonAsync("/seller/auth/refresh", request);
        resp.EnsureSuccessStatusCode();
        return ParseAuth(await resp.Content.ReadAsStringAsync());
    }

    /// <summary>
    /// Extrait le jeton de la réponse d'auth de façon tolérante : noms de champs
    /// variés (accessToken / access_token / token / jwt…), wrapper éventuel
    /// (data / result), et expiration en date ISO ou en secondes (expiresIn).
    /// </summary>
    /// <summary>Parseur tolérant de la réponse d'auth (login/refresh). Public pour
    /// être réutilisé par AuthState lors du rafraîchissement du jeton.</summary>
    public static AuthResult ParseAuth(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new AuthResult("", "", DateTime.UtcNow, "");

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (root.ValueKind == JsonValueKind.Object)
            foreach (var wrap in new[] { "data", "result", "payload", "tokens" })
                if (root.TryGetProperty(wrap, out var inner) && inner.ValueKind == JsonValueKind.Object)
                    root = inner;

        var access = FindString(root, "accessToken", "access_token", "token", "jwt", "idToken") ?? "";
        var refresh = FindString(root, "refreshToken", "refresh_token") ?? "";
        var seller = FindString(root, "sellerName", "shopName", "name", "displayName", "fullName") ?? "";

        DateTime? exp = null;
        if (DateTime.TryParse(FindString(root, "accessTokenExpiresOnUtc", "expiresAt", "expires_at", "expiry", "expiration"), out var d))
            exp = d;
        else if (FindNumber(root, out var secs, "expiresIn", "expires_in"))
            exp = DateTime.UtcNow.AddSeconds(secs);

        return new AuthResult(access, refresh, exp ?? DateTime.UtcNow.AddHours(1), seller);
    }

    private static string Norm(string s) => s.Replace("_", "").Replace("-", "").ToLowerInvariant();

    private static string? FindString(JsonElement obj, params string[] names)
    {
        if (obj.ValueKind != JsonValueKind.Object) return null;
        foreach (var p in obj.EnumerateObject())
            foreach (var n in names)
                if (Norm(p.Name) == Norm(n) && p.Value.ValueKind == JsonValueKind.String)
                    return p.Value.GetString();
        return null;
    }

    private static bool FindNumber(JsonElement obj, out double value, params string[] names)
    {
        value = 0;
        if (obj.ValueKind != JsonValueKind.Object) return false;
        foreach (var p in obj.EnumerateObject())
            foreach (var n in names)
                if (Norm(p.Name) == Norm(n) && p.Value.ValueKind == JsonValueKind.Number)
                {
                    value = p.Value.GetDouble();
                    return true;
                }
        return false;
    }

    public Task LogoutAsync(LogoutRequest request)
        => _http.PostAsJsonAsync("/seller/auth/logout", request);

    // ---------- Boutique ----------
    public async Task<Shop> GetShopAsync()
        => (await _http.GetFromJsonAsync<Shop>("/seller/shop"))!;

    public async Task<Shop> OnboardAsync(OnboardRequest request)
        => await PostJson<Shop>("/seller/shop", request);

    public Task UpdateProfileAsync(ProfileRequest request)
        => _http.PutAsJsonAsync("/seller/shop/profile", request);

    public Task SetPayoutAccountAsync(PayoutAccountRequest request)
        => _http.PutAsJsonAsync("/seller/shop/payout-account", request);

    public Task UploadKybDocumentAsync(KybDocumentRequest request)
        => _http.PostAsJsonAsync("/seller/shop/kyb-documents", request);

    public async Task UploadKybDocumentFileAsync(string type, string fileName, string contentType, byte[] content)
    {
        using var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(content);
        file.Headers.ContentType = new MediaTypeHeaderValue(string.IsNullOrEmpty(contentType) ? "application/octet-stream" : contentType);
        form.Add(file, "file", fileName);
        form.Add(new StringContent(type), "type");
        var resp = await _http.PostAsync("/seller/shop/kyb-documents/upload", form);
        resp.EnsureSuccessStatusCode();
    }

    public async Task<string?> GetKybDownloadUrlAsync(Guid documentId)
    {
        var resp = await _http.GetFromJsonAsync<DownloadUrlResponse>($"/seller/shop/kyb-documents/{documentId}/download");
        return resp?.Url;
    }

    public async Task DeleteKybDocumentAsync(Guid documentId)
    {
        var resp = await _http.DeleteAsync($"/seller/shop/kyb-documents/{documentId}");
        resp.EnsureSuccessStatusCode();
    }

    public async Task<string?> SetShopLogoAsync(string fileName, string contentType, byte[] content)
    {
        using var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(content);
        file.Headers.ContentType = new MediaTypeHeaderValue(string.IsNullOrEmpty(contentType) ? "image/png" : contentType);
        form.Add(file, "file", fileName);
        var resp = await _http.PostAsync("/seller/shop/logo", form);
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadFromJsonAsync<LogoUrlResponse>();
        return body?.LogoUrl;
    }

    private sealed record DownloadUrlResponse(string Url);
    private sealed record LogoUrlResponse(string LogoUrl);

    // ---------- Catalogue ----------
    public async Task<IReadOnlyList<SellerCategory>> ListCategoriesAsync()
        => (await _http.GetFromJsonAsync<List<SellerCategory>>("/seller/categories")) ?? new();

    public async Task<IReadOnlyList<SellerBrand>> ListBrandsAsync()
        => (await _http.GetFromJsonAsync<List<SellerBrand>>("/seller/brands")) ?? new();

    public async Task<CatalogProduct> GetProductAsync(Guid id)
        => (await _http.GetFromJsonAsync<CatalogProduct>($"/seller/products/{id}"))!;

    public async Task<CatalogProduct> CreateProductAsync(CreateProductRequest request, IReadOnlyList<ProductImageUpload> images)
    {
        using var form = new MultipartFormDataContent();

        foreach (var img in images)
        {
            var file = new ByteArrayContent(img.Content);
            file.Headers.ContentType = new MediaTypeHeaderValue(img.ContentType);
            form.Add(file, "images", img.FileName);   // tableau « images »
        }

        form.Add(new StringContent(request.CategoryId.ToString()), "categoryId");
        form.Add(new StringContent(request.Name ?? string.Empty), "name");
        form.Add(new StringContent(request.Description ?? string.Empty), "description");
        if (request.BrandId is not null) form.Add(new StringContent(request.BrandId.Value.ToString()), "brandId");
        if (!string.IsNullOrEmpty(request.Gtin)) form.Add(new StringContent(request.Gtin), "gtin");
        if (!string.IsNullOrEmpty(request.Ean)) form.Add(new StringContent(request.Ean), "ean");
        if (request.ProductGroupId is not null) form.Add(new StringContent(request.ProductGroupId.Value.ToString()), "productGroupId");
        if (request.Tags is { Count: > 0 })
            form.Add(new StringContent(string.Join(",", request.Tags)), "tags");
        if (request.Attributes is { Count: > 0 })
            form.Add(new StringContent(JsonSerializer.Serialize(request.Attributes)), "attributesJson");

        var resp = await _http.PostAsync("/seller/products", form);
        resp.EnsureSuccessStatusCode();

        var json = await resp.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(json))
            return new CatalogProduct { Id = Guid.NewGuid(), Name = request.Name ?? "" };

        // L'id renvoyé peut être direct ou enveloppé ; extraction tolérante.
        using var doc = JsonDocument.Parse(json);
        var idStr = FindString(doc.RootElement, "id", "productId", "guid");
        var id = Guid.TryParse(idStr, out var g) ? g : Guid.NewGuid();
        return new CatalogProduct { Id = id, Name = request.Name ?? "" };
    }

    public async Task<ProductImageUpload> ProcessImageAsync(ProductImageUpload image)
    {
        using var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(image.Content);
        file.Headers.ContentType = new MediaTypeHeaderValue(
            string.IsNullOrWhiteSpace(image.ContentType) ? "application/octet-stream" : image.ContentType);
        form.Add(file, "image", image.FileName);

        var resp = await _http.PostAsync("/seller/products/media/process", form);
        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync();
            if (body.Length > 200) body = body[..200];
            throw new HttpRequestException($"HTTP {(int)resp.StatusCode} — {body}");
        }

        var bytes = await resp.Content.ReadAsByteArrayAsync();
        var contentType = resp.Content.Headers.ContentType?.MediaType ?? "image/jpeg";
        var baseName = System.IO.Path.GetFileNameWithoutExtension(image.FileName);
        // L'image traitée est un JPEG à fond blanc.
        return new ProductImageUpload
        {
            FileName = $"{(string.IsNullOrWhiteSpace(baseName) ? "image" : baseName)}.jpg",
            ContentType = contentType,
            Content = bytes,
            AltText = image.AltText,
        };
    }

    public Task UpdateProductAsync(Guid id, UpdateProductRequest request)
        => _http.PutAsJsonAsync($"/seller/products/{id}", request);

    public Task SetProductStatusAsync(Guid id, CatalogStatusRequest request)
        => _http.PatchAsJsonAsync($"/seller/products/{id}/status", request);

    public Task AddMediaAsync(Guid id, MediaRequest request)
        => _http.PostAsJsonAsync($"/seller/products/{id}/media", request);

    // ---------- Offres ----------
    public async Task<IReadOnlyList<Offer>> ListOffersAsync()
        => (await _http.GetFromJsonAsync<List<Offer>>("/seller/offers")) ?? new();

    public async Task<Offer> GetOfferAsync(Guid id)
        => (await _http.GetFromJsonAsync<Offer>($"/seller/offers/{id}"))!;

    public async Task<Offer> CreateOfferAsync(CreateOfferRequest request)
        => await PostJson<Offer>("/seller/offers", request);

    public Task SetPriceAsync(Guid id, OfferPriceRequest request)
        => _http.PatchAsJsonAsync($"/seller/offers/{id}/price", request);

    public Task SetStatusAsync(Guid id, OfferStatusRequest request)
        => _http.PatchAsJsonAsync($"/seller/offers/{id}/status", request);

    public Task SetHandlingTimeAsync(Guid id, HandlingTimeRequest request)
        => _http.PatchAsJsonAsync($"/seller/offers/{id}/handling-time", request);

    // ---------- Exécution ----------
    public async Task<IReadOnlyList<Shipment>> ListShipmentsAsync()
        => (await _http.GetFromJsonAsync<List<Shipment>>("/seller/shipments")) ?? new();

    public async Task<Shipment> GetShipmentAsync(Guid id)
        => (await _http.GetFromJsonAsync<Shipment>($"/seller/shipments/{id}"))!;

    public async Task<IReadOnlyList<Shipment>> GetByOrderAsync(Guid orderId)
        => (await _http.GetFromJsonAsync<List<Shipment>>($"/seller/shipments/by-order/{orderId}")) ?? new();

    public Task PrepareAsync(Guid id)
        => _http.PostAsync($"/seller/shipments/{id}/prepare", null);

    public Task ShipAsync(Guid id, ShipRequest request)
        => _http.PostAsJsonAsync($"/seller/shipments/{id}/ship", request);

    public Task DeliverAsync(Guid id)
        => _http.PostAsync($"/seller/shipments/{id}/deliver", null);

    public Task CancelAsync(Guid id)
        => _http.PostAsync($"/seller/shipments/{id}/cancel", null);

    // ---------- Commandes ----------
    public async Task<IReadOnlyList<SellerOrder>> ListOrdersAsync()
        => (await _http.GetFromJsonAsync<List<SellerOrder>>("/seller/orders")) ?? new();

    public async Task<SellerOrder?> GetOrderAsync(Guid id)
        => await _http.GetFromJsonAsync<SellerOrder>($"/seller/orders/{id}");

    // ---------- Tableau de bord ----------
    public async Task<SellerDashboardKpis> GetDashboardAsync()
        => (await _http.GetFromJsonAsync<SellerDashboardKpis>("/seller/dashboard")) ?? new();

    // ---------- Finances ----------
    public async Task<FinanceStatement> GetStatementAsync(DateTime? from = null, DateTime? to = null)
    {
        // Format « s » (yyyy-MM-ddTHH:mm:ss, invariant, sans fuseau) + encodage URL
        // des « : » : garantit un binding DateTime? fiable côté BFF.
        var q = new List<string>();
        if (from is not null) q.Add($"from={Uri.EscapeDataString(from.Value.ToString("s"))}");
        if (to is not null) q.Add($"to={Uri.EscapeDataString(to.Value.ToString("s"))}");
        var url = "/seller/finance/statement" + (q.Count > 0 ? "?" + string.Join("&", q) : "");

        var resp = await _http.GetAsync(url);
        if (!resp.IsSuccessStatusCode)
        {
            // Remonte le corps de la réponse (ex. { "error": "invalid_period" }) pour diagnostic.
            var body = await resp.Content.ReadAsStringAsync();
            throw new HttpRequestException($"HTTP {(int)resp.StatusCode} — {body}", null, resp.StatusCode);
        }
        return (await resp.Content.ReadFromJsonAsync<FinanceStatement>())!;
    }

    public async Task<IReadOnlyList<Payout>> ListPayoutsAsync()
        => (await _http.GetFromJsonAsync<List<Payout>>("/seller/finance/payouts")) ?? new();

    // ---------- Messagerie ----------
    public async Task<IReadOnlyList<Conversation>> ListConversationsAsync()
        => (await _http.GetFromJsonAsync<List<Conversation>>("/seller/conversations")) ?? new();

    public async Task<IReadOnlyList<Message>> GetMessagesAsync(Guid conversationId)
        => (await _http.GetFromJsonAsync<List<Message>>($"/seller/conversations/{conversationId}/messages")) ?? new();

    public async Task<Message> SendAsync(Guid conversationId, SendRequest request)
        => await PostJson<Message>($"/seller/conversations/{conversationId}/messages", request);

    // ---------- Avis ----------
    public async Task<IReadOnlyList<Review>> ListReviewsAsync()
        => (await _http.GetFromJsonAsync<List<Review>>("/seller/reviews")) ?? new();

    public async Task<IReadOnlyList<Review>> GetProductReviewsAsync(Guid productId)
        => (await _http.GetFromJsonAsync<List<Review>>($"/seller/reviews/products/{productId}")) ?? new();

    public async Task<ProductRating> GetProductRatingAsync(Guid productId)
        => (await _http.GetFromJsonAsync<ProductRating>($"/seller/reviews/products/{productId}/rating"))!;

    public Task FlagAsync(Guid reviewId)
        => _http.PostAsync($"/seller/reviews/{reviewId}/flag", null);

    public Task ReplyAsync(Guid reviewId, string body)
        => _http.PostAsJsonAsync($"/seller/reviews/{reviewId}/reply", new { body });

    // ---------- Helper ----------
    private async Task<T> PostJson<T>(string url, object body)
    {
        var resp = await _http.PostAsJsonAsync(url, body);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<T>())!;
    }
}
