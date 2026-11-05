
using ITHelpdesk.Web.Models.Entities;
using ITHelpdesk.Web.Models.Enums;
using ITHelpdesk.Web.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ITHelpdesk.Web.Controllers;

public class AccountController : Controller
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;

    public AccountController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager)
    {
        _signInManager = signInManager;
        _userManager = userManager;
    }

    // ==========================================
    // LOGIN PAGE
    // ==========================================

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToLocal(returnUrl);
        }

        var model = new LoginViewModel
        {
            ReturnUrl = returnUrl
        };

        return View(model);
    }

    // ==========================================
    // LOGIN PROCESS
    // ==========================================

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToLocal(model.ReturnUrl);
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var identifier = model.LoginIdentifier.Trim();

        // Find account using email first.
        var user = await _userManager.FindByEmailAsync(identifier);

        // If email is not found, search using username.
        if (user == null)
        {
            user = await _userManager.FindByNameAsync(identifier);
        }

        if (user == null)
        {
            ModelState.AddModelError(
                string.Empty,
                "Email/username atau password tidak valid."
            );

            return View(model);
        }

        // Check account approval.
        if (user.ApprovalStatus != AccountApprovalStatus.Approved)
        {
            ModelState.AddModelError(
                string.Empty,
                "Akun belum disetujui atau tidak dapat digunakan. Silakan hubungi Admin."
            );

            return View(model);
        }

        // Check account active status.
        if (!user.IsActive)
        {
            ModelState.AddModelError(
                string.Empty,
                "Akun sedang dinonaktifkan. Silakan hubungi Admin."
            );

            return View(model);
        }

        // Verify password and sign in.
        var result = await _signInManager.PasswordSignInAsync(
            user,
            model.Password,
            model.RememberMe,
            lockoutOnFailure: true
        );

        if (result.Succeeded)
        {
            return RedirectToLocal(model.ReturnUrl);
        }

        if (result.IsLockedOut)
        {
            ModelState.AddModelError(
                string.Empty,
                "Akun terkunci sementara karena terlalu banyak percobaan login."
            );

            return View(model);
        }

        ModelState.AddModelError(
            string.Empty,
            "Email/username atau password tidak valid."
        );

        return View(model);
    }

    // ==========================================
    // REGISTER PAGE
    // ==========================================

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }

        return View(new RegisterViewModel());
    }

    // ==========================================
    // REGISTER PROCESS
    // ==========================================

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var email = model.Email.Trim();
        var userName = model.UserName.Trim();

        var user = new ApplicationUser
        {
            UserName = userName,
            Email = email,
            FullName = model.FullName.Trim(),
            EmailConfirmed = false,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
            ApprovalStatus = AccountApprovalStatus.Pending
        };

        var result = await _userManager.CreateAsync(
            user,
            model.Password
        );

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(
                    string.Empty,
                    error.Description
                );
            }

            return View(model);
        }

        return RedirectToAction(nameof(RegistrationPending));
    }

    // ==========================================
    // REGISTRATION PENDING PAGE
    // ==========================================

    [AllowAnonymous]
    [HttpGet]
    public IActionResult RegistrationPending()
    {
        return View();
    }

    // ==========================================
    // LOGOUT
    // ==========================================

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();

        return RedirectToAction(nameof(Login));
    }

    // ==========================================
    // SAFE REDIRECT
    // ==========================================

    private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (!string.IsNullOrEmpty(returnUrl) &&
            Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }

        return RedirectToAction("Index", "Home");
    }
}
