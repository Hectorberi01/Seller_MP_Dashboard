namespace Seller_MP_Dashboard.Models;

/// <summary>Fichier image sélectionné pour l'upload (POST /seller/products multipart).</summary>
public class ProductImageUpload
{
    public required string FileName { get; init; }
    public required string ContentType { get; init; }
    public required byte[] Content { get; init; }

    /// <summary>Texte alternatif (accessibilité / SEO), optionnel.</summary>
    public string? AltText { get; init; }

    /// <summary>Aperçu data-URL (affichage avant envoi).</summary>
    public string DataUrl => $"data:{ContentType};base64,{System.Convert.ToBase64String(Content)}";
}

/// <summary>Modèle de saisie pour la création d'un produit.</summary>
public class ProductForm
{
    public string Name { get; set; } = "";
    /// <summary>Identifiant de la catégorie choisie (catalogue de la plateforme).</summary>
    public Guid? CategoryId { get; set; }
    public string Description { get; set; } = "";

    /// <summary>Marque (optionnel) — catalogue de la plateforme.</summary>
    public Guid? BrandId { get; set; }
    public string Gtin { get; set; } = "";
    public string Ean { get; set; } = "";
    /// <summary>Groupe de produit / déclinaisons (optionnel).</summary>
    public Guid? ProductGroupId { get; set; }
    /// <summary>Tags libres (un par ligne / séparés par virgule).</summary>
    public List<string> Tags { get; set; } = new();
    /// <summary>Attributs personnalisés (clé/valeur).</summary>
    public List<ProductAttribute> Attributes { get; set; } = new();

    /// <summary>Fichiers images sélectionnés (un produit peut en avoir plusieurs).</summary>
    public List<ProductImageUpload> Images { get; set; } = new();
}

/// <summary>Une paire attribut clé/valeur pour la saisie produit.</summary>
public class ProductAttribute
{
    public string Key { get; set; } = "";
    public string Value { get; set; } = "";
}

/// <summary>Modèle de saisie pour la création d'une offre.</summary>
public class OfferForm
{
    public string ProductName { get; set; } = "";
    public string Sku { get; set; } = "";
    public double PriceXof { get; set; }
    public string Condition { get; set; } = "new";
    public int HandlingTime { get; set; } = 2;
}
