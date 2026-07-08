namespace Seller_MP_Dashboard.Models;

public class KpiStat
{
    public required string Label { get; init; }
    public required string Value { get; init; }
    public string? Sublabel { get; init; }
    /// <summary>Variation en % vs période précédente (peut être négative).</summary>
    public double? TrendPercent { get; init; }
    public string Icon { get; init; } = "📊";
    public bool TrendIsGood => TrendPercent is null || TrendPercent >= 0;
}
