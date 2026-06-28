using System.ComponentModel.DataAnnotations;

namespace Seller_MP_Dashboard.Models;

public class LoginModel
{
    [Required, EmailAddress]
    public string Email { get; set; } = "";

    [Required]
    public string Password { get; set; } = "";

    public string? MfaCode { get; set; }
}
