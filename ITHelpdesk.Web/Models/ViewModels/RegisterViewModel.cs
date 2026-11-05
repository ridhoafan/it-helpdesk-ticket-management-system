
using System.ComponentModel.DataAnnotations;

namespace ITHelpdesk.Web.Models.ViewModels;

public class RegisterViewModel
{
    // ==========================================
    // FULL NAME
    // ==========================================

    [Required(ErrorMessage = "Nama lengkap wajib diisi.")]
    [StringLength(
        150,
        MinimumLength = 3,
        ErrorMessage = "Nama lengkap harus terdiri dari 3-150 karakter."
    )]
    [Display(Name = "Nama Lengkap")]
    public string FullName { get; set; } = string.Empty;

    // ==========================================
    // USERNAME
    // ==========================================

    [Required(ErrorMessage = "Username wajib diisi.")]
    [StringLength(
        30,
        MinimumLength = 3,
        ErrorMessage = "Username harus terdiri dari 3-30 karakter."
    )]
    [RegularExpression(
        @"^[a-zA-Z0-9._-]+$",
        ErrorMessage = "Username hanya boleh berisi huruf, angka, titik, underscore, dan strip."
    )]
    [Display(Name = "Username")]
    public string UserName { get; set; } = string.Empty;

    // ==========================================
    // EMAIL
    // ==========================================

    [Required(ErrorMessage = "Email wajib diisi.")]
    [EmailAddress(ErrorMessage = "Format email tidak valid.")]
    [StringLength(256)]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    // ==========================================
    // PASSWORD
    // ==========================================

    [Required(ErrorMessage = "Password wajib diisi.")]
    [StringLength(
        100,
        MinimumLength = 8,
        ErrorMessage = "Password minimal 8 karakter."
    )]
    [RegularExpression(
        @"^(?=.*[a-z])(?=.*\d).+$",
        ErrorMessage = "Password harus memiliki minimal satu huruf kecil dan satu angka."
    )]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;

    // ==========================================
    // CONFIRM PASSWORD
    // ==========================================

    [Required(ErrorMessage = "Konfirmasi password wajib diisi.")]
    [DataType(DataType.Password)]
    [Compare(
        nameof(Password),
        ErrorMessage = "Konfirmasi password tidak cocok."
    )]
    [Display(Name = "Konfirmasi Password")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
