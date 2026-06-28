namespace Seller_MP_Dashboard.Models;

public class OrderLine
{
    public required string ProductName { get; init; }
    public string Emoji { get; init; } = "";
    public int Qty { get; init; } = 1;
    public int UnitPriceXof { get; init; }
    public int LineTotalXof => Qty * UnitPriceXof;
}
