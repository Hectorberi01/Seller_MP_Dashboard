namespace Seller_MP_Dashboard.Models;

public class Order
{
    public required string Reference { get; init; }   // ex. CMD-10428
    public required string Customer { get; init; }
    public DateTime Date { get; init; }
    public int TotalXof { get; init; }
    public OrderStatus Status { get; init; }
    public PaymentStatus Payment { get; init; }

    /// <summary>Lignes de la commande.</summary>
    public List<OrderLine> Items { get; init; } = new();

    /// <summary>Nombre d'articles (somme des quantités).</summary>
    public int ItemCount => Items.Sum(i => i.Qty);

    // Coordonnées de livraison (démo)
    public string? ShippingCity { get; init; }
    public string? ShippingAddress { get; init; }
}
