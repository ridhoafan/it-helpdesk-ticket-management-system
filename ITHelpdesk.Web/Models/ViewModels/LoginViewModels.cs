using System.ComponentModel.DataAnnotations;

namespace ITHelpdesk.Web.Models.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "Email atau username wajib diisi.")]
    [Display(Name = "Email atau Username")]
    public string LoginIdentifier { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password wajib diisi.")]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Ingat saya")]
    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}