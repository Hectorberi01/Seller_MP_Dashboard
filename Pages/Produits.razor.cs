using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Seller_MP_Dashboard.Api;
using Seller_MP_Dashboard.Models;

namespace Seller_MP_Dashboard.Pages;

public partial class Produits
{
    [Inject] ISellerApi Api { get; set; } = default!;
    [Inject] NavigationManager Nav { get; set; } = default!;

    private const long MaxImageBytes = 5 * 1024 * 1024; // 5 Mo / image

    private IReadOnlyList<Product>? _products;
    private string? _loadError;

    private StockStatus? _filter;
    private string _search = "";
    private int _page = 1;
    private const int PageSize = 15;

    private List<Product> Filtered =>
        (_products ?? Array.Empty<Product>())
            .Where(p => _filter is null || p.StockStatus == _filter)
            .Where(p => string.IsNullOrWhiteSpace(_search)
                     || p.Name.Contains(_search.Trim(), StringComparison.OrdinalIgnoreCase)
                     || (p.Sku ?? "").Contains(_search.Trim(), StringComparison.OrdinalIgnoreCase))
            .ToList();

    private int PageCount => Math.Max(1, (int)Math.Ceiling(Filtered.Count / (double)PageSize));
    private IEnumerable<Product> Paged => Filtered.Skip((Math.Min(_page, PageCount) - 1) * PageSize).Take(PageSize);

    private void SetStockFilter(StockStatus? status) { _filter = status; _page = 1; }
    private void OnSearchChanged(ChangeEventArgs e) { _search = e.Value?.ToString() ?? ""; _page = 1; }
    private void PrevPage() { if (_page > 1) _page--; }
    private void NextPage() { if (_page < PageCount) _page++; }

    private IReadOnlyList<SellerCategory>? _categories;
    private IReadOnlyList<SellerBrand>? _brands;

    // Prix le plus bas par produit (depuis les offres), avec la devise de l'offre.
    private readonly Dictionary<Guid, (double Price, string Currency)> _offerPrice = new();

    // --- Création produit ---
    private bool _showCreate;
    private string? _formError;
    private ProductForm _form = new();
    private string _tagsText = "";
    private string _productGroupIdText = "";

    protected override async Task OnInitializedAsync()
    {
        await Load();
        await LoadCategories();
        await LoadBrands();
        await LoadOfferPrices();
        await LoadStockTotals();
    }

    // Stock réel par produit = somme des disponibilités de ses variantes (par SKU).
    private async Task LoadStockTotals()
    {
        if (_products is null) return;
        foreach (var p in _products)
        {
            var total = 0;
            foreach (var sku in p.Variants.Select(v => v.Sku).Where(s => !string.IsNullOrWhiteSpace(s)).Distinct())
            {
                try
                {
                    var items = await Api.ListInventoryBySkuAsync(sku);
                    total += items.Sum(i => i.Available);
                }
                catch { /* SKU sans stock : ignoré */ }
            }
            p.Stock = total;
        }
        StateHasChanged();
    }

    private async Task LoadOfferPrices()
    {
        _offerPrice.Clear();
        try
        {
            foreach (var grp in (await Api.ListOffersAsync()).GroupBy(o => o.ProductId))
            {
                var best = grp.OrderBy(o => o.BasePriceAmount).First();
                _offerPrice[grp.Key] = (best.BasePriceAmount, best.Currency);
            }
        }
        catch { /* prix optionnel */ }
    }

    private string CategoryName(Product p)
    {
        var cat = _categories?.FirstOrDefault(c => c.Id == p.CategoryId);
        return cat is null ? "—" : (string.IsNullOrWhiteSpace(cat.Name) ? cat.Display : cat.Name);
    }

    private string PriceLabel(Product p)
        => _offerPrice.TryGetValue(p.Id, out var info)
            ? $"{info.Price:N0} {info.Currency}"
            : "—";

    private async Task Load()
    {
        try
        {
            _loadError = null;
            _products = await Api.ListProductsAsync();
        }
        catch
        {
            _loadError = "Impossible de joindre le serveur (BFF injoignable ou session expirée).";
        }
    }

    private async Task LoadCategories()
    {
        try { _categories = await Api.ListCategoriesAsync(); }
        catch { _categories = Array.Empty<SellerCategory>(); }
    }

    private async Task LoadBrands()
    {
        try { _brands = await Api.ListBrandsAsync(); }
        catch { _brands = Array.Empty<SellerBrand>(); }
    }

    private async Task OnFilesSelected(InputFileChangeEventArgs e)
    {
        foreach (var file in e.GetMultipleFiles(maximumFileCount: 10))
        {
            using var ms = new MemoryStream();
            await file.OpenReadStream(MaxImageBytes).CopyToAsync(ms);
            _form.Images.Add(new ProductImageUpload
            {
                FileName = file.Name,
                ContentType = string.IsNullOrEmpty(file.ContentType) ? "image/jpeg" : file.ContentType,
                Content = ms.ToArray()
            });
        }
    }

    private void RemoveImage(int index)
    {
        if (index >= 0 && index < _form.Images.Count)
            _form.Images.RemoveAt(index);
    }

    private void SetPrimary(int index)
    {
        if (index <= 0 || index >= _form.Images.Count) return;
        var img = _form.Images[index];
        _form.Images.RemoveAt(index);
        _form.Images.Insert(0, img); // la 1re image est l'image principale
    }

    private async Task CreateProduct()
    {
        if (string.IsNullOrWhiteSpace(_form.Name))
        {
            _formError = "Le nom du produit est requis.";
            return;
        }
        if (string.IsNullOrWhiteSpace(_form.Description))
        {
            _formError = "La description est requise.";
            return;
        }
        if (_form.CategoryId is null || _form.CategoryId == Guid.Empty)
        {
            _formError = "Veuillez choisir une catégorie.";
            return;
        }

        Guid? productGroupId = null;
        if (!string.IsNullOrWhiteSpace(_productGroupIdText))
        {
            if (!Guid.TryParse(_productGroupIdText.Trim(), out var pgid)) { _formError = "Le groupe de produit doit être un GUID valide."; return; }
            productGroupId = pgid;
        }

        var tags = _tagsText
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

        var attributes = _form.Attributes
            .Where(a => !string.IsNullOrWhiteSpace(a.Key))
            .GroupBy(a => a.Key.Trim())
            .ToDictionary(g => g.Key, g => g.Last().Value?.Trim() ?? "");

        // POST /seller/products (multipart) : fichiers images + champs (categoryId réel).
        var request = new CreateProductRequest(
            CategoryId: _form.CategoryId.Value,
            Name: _form.Name.Trim(),
            Description: _form.Description.Trim(),
            BrandId: _form.BrandId is { } b && b != Guid.Empty ? b : null,
            Gtin: string.IsNullOrWhiteSpace(_form.Gtin) ? null : _form.Gtin.Trim(),
            Ean: string.IsNullOrWhiteSpace(_form.Ean) ? null : _form.Ean.Trim(),
            ProductGroupId: productGroupId,
            Attributes: attributes.Count > 0 ? attributes : null,
            Tags: tags.Count > 0 ? tags : null);

        try
        {
            await Api.CreateProductAsync(request, _form.Images);
        }
        catch (Exception ex)
        {
            _formError = $"Échec de la création : {ex.Message}";
            return;
        }

        await Load();
        _form = new();
        _tagsText = "";
        _productGroupIdText = "";
        _formError = null;
        _showCreate = false;
    }

    // --- Suppression (avec confirmation) ---
    private Product? _deleteTarget;
    private bool _deleting;
    private string? _deleteError;

    private void AskDelete(Product p)
    {
        _deleteError = null;
        _deleteTarget = p;
    }

    private async Task ConfirmDelete()
    {
        if (_deleteTarget is null) return;
        _deleting = true;
        _deleteError = null;
        try
        {
            await Api.DeleteProductAsync(_deleteTarget.Id);
            _deleteTarget = null;
            await Load();
        }
        catch (Exception ex) { _deleteError = $"Échec de la suppression : {ex.Message}"; }
        finally { _deleting = false; }
    }
}
