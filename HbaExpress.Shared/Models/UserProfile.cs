namespace Seller_MP_Dashboard.Models;

public class UserProfile
{
    public required string FullName { get; set; }
    public required string Email { get; set; }
    public string? Phone { get; set; }
    public string Role { get; set; } = "Vendeur";
    public string ShopName { get; set; } = "";
    public DateTime MemberSince { get; set; }
    public bool MfaEnabled { get; set; }
    public string Language { get; set; } = "Français";
    public string Initials =>
        string.Concat(FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Take(2).Select(p => char.ToUpper(p[0])));
}
