using ITHelpdesk.Web.Data;
using ITHelpdesk.Web.Models.Entities;
using ITHelpdesk.Web.Models.Enums;
using ITHelpdesk.Web.Models.ViewModels;
using ITHelpdesk.Web.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ITHelpdesk.Web.Controllers;

[Authorize(Roles = "Admin")]
public class UserApprovalsController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IAuditLogService _auditLogService;
    private readonly AppDbContext _context;

    public UserApprovalsController(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IAuditLogService auditLogService,
        AppDbContext context)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _auditLogService = auditLogService;
        _context = context;
    }

    // ==========================================
    // PENDING USER LIST
    // ==========================================

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var pendingUsers = await _userManager.Users
            .AsNoTracking()
            .Where(u => u.ApprovalStatus == AccountApprovalStatus.Pending)
            .OrderBy(u => u.CreatedAtUtc)
            .Select(u => new PendingUserItemViewModel
            {
                Id = u.Id,
                FullName = u.FullName,
                UserName = u.UserName ?? string.Empty,
                Email = u.Email ?? string.Empty,
                CreatedAtUtc = u.CreatedAtUtc
            })
            .ToListAsync();

        // Get active ticket categories for the approval form.
        ViewBag.Categories = await _context.TicketCategories
            .AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync();

        var model = new UserApprovalIndexViewModel
        {
            PendingUsers = pendingUsers
        };

        return View(model);
    }

    // ==========================================
    // APPROVE USER
    // ==========================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(
        string id,
        string role,
        string? note,
        int[]? categoryIds)
    {
        if (role != "Employee" && role != "IT Support")
        {
            TempData["ErrorMessage"] =
                "Role yang dipilih tidak valid.";

            return RedirectToAction(nameof(Index));
        }

        if (note?.Length > 500)
        {
            TempData["ErrorMessage"] =
                "Catatan maksimal 500 karakter.";

            return RedirectToAction(nameof(Index));
        }

        var selectedCategoryIds = (categoryIds ?? Array.Empty<int>())
            .Distinct()
            .ToArray();

        // IT Support must have at least one specialization.
        if (role == "IT Support" && selectedCategoryIds.Length == 0)
        {
            TempData["ErrorMessage"] =
                "Pilih minimal satu spesialisasi untuk IT Support.";

            return RedirectToAction(nameof(Index));
        }

        var admin = await _userManager.GetUserAsync(User);

        if (admin == null)
        {
            return Challenge();
        }

        var user = await _userManager.FindByIdAsync(id);

        if (user == null)
        {
            TempData["ErrorMessage"] =
                "Pengguna tidak ditemukan.";

            return RedirectToAction(nameof(Index));
        }

        if (user.Id == admin.Id)
        {
            TempData["ErrorMessage"] =
                "Admin tidak dapat menyetujui akunnya sendiri.";

            return RedirectToAction(nameof(Index));
        }

        if (user.ApprovalStatus != AccountApprovalStatus.Pending)
        {
            TempData["ErrorMessage"] =
                "Akun ini sudah diproses sebelumnya.";

            return RedirectToAction(nameof(Index));
        }

        if (!await _roleManager.RoleExistsAsync(role))
        {
            TempData["ErrorMessage"] =
                "Role belum tersedia di sistem.";

            return RedirectToAction(nameof(Index));
        }

        // ==========================================
        // VALIDATE SPECIALIZATION CATEGORIES
        // ==========================================

        var selectedCategories = new List<TicketCategory>();

        if (role == "IT Support")
        {
            selectedCategories = await _context.TicketCategories
                .Where(c =>
                    selectedCategoryIds.Contains(c.Id) &&
                    c.IsActive)
                .ToListAsync();

            if (selectedCategories.Count != selectedCategoryIds.Length)
            {
                TempData["ErrorMessage"] =
                    "Salah satu kategori tidak valid atau tidak aktif.";

                return RedirectToAction(nameof(Index));
            }
        }

        // ==========================================
        // START DATABASE TRANSACTION
        // ==========================================

        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        // Remove existing roles before assigning the selected role.
        var existingRoles = await _userManager.GetRolesAsync(user);

        if (existingRoles.Count > 0)
        {
            var removeResult = await _userManager.RemoveFromRolesAsync(
                user,
                existingRoles
            );

            if (!removeResult.Succeeded)
            {
                await transaction.RollbackAsync();

                TempData["ErrorMessage"] =
                    "Gagal memperbarui role pengguna.";

                return RedirectToAction(nameof(Index));
            }
        }

        // Assign the role selected by Admin.
        var roleResult = await _userManager.AddToRoleAsync(user, role);

        if (!roleResult.Succeeded)
        {
            await transaction.RollbackAsync();

            TempData["ErrorMessage"] =
                "Gagal memberikan role kepada pengguna.";

            return RedirectToAction(nameof(Index));
        }

        // ==========================================
        // UPDATE ACCOUNT APPROVAL INFORMATION
        // ==========================================

        user.ApprovalStatus = AccountApprovalStatus.Approved;
        user.IsActive = true;
        user.ApprovalReviewedAtUtc = DateTime.UtcNow;
        user.ApprovalReviewedByUserId = admin.Id;
        user.ApprovalNote = string.IsNullOrWhiteSpace(note)
            ? null
            : note.Trim();

        var updateResult = await _userManager.UpdateAsync(user);

        if (!updateResult.Succeeded)
        {
            await transaction.RollbackAsync();

            TempData["ErrorMessage"] =
                "Gagal menyimpan persetujuan akun.";

            return RedirectToAction(nameof(Index));
        }

        // ==========================================
        // SAVE IT SUPPORT SPECIALIZATIONS
        // ==========================================

        var existingSpecializations = await _context.ITSupportCategories
            .Where(sc => sc.ITSupportUserId == user.Id)
            .ToListAsync();

        _context.ITSupportCategories.RemoveRange(
            existingSpecializations
        );

        if (role == "IT Support")
        {
            var specializations = selectedCategories.Select(category =>
                new ITSupportCategory
                {
                    ITSupportUserId = user.Id,
                    TicketCategoryId = category.Id,
                    AssignedAtUtc = DateTime.UtcNow
                });

            await _context.ITSupportCategories.AddRangeAsync(
                specializations
            );
        }

        await _context.SaveChangesAsync();

        // ==========================================
        // RECORD AUDIT LOG
        // ==========================================

        var specializationDescription = role == "IT Support"
            ? " Specializations: " +
              string.Join(", ", selectedCategories.Select(c => c.Name)) +
              "."
            : string.Empty;

        await _auditLogService.LogAsync(
            admin.Id,
            "UserApproved",
            "User",
            user.Id,
            $"Admin approved registration for {user.FullName} " +
            $"({user.Email}) and assigned role {role}." +
            specializationDescription,
            HttpContext.Connection.RemoteIpAddress?.ToString()
        );

        await transaction.CommitAsync();

        TempData["SuccessMessage"] =
            $"Akun {user.FullName} berhasil disetujui dengan role {role}.";

        return RedirectToAction(nameof(Index));
    }

    // ==========================================
    // REJECT USER
    // ==========================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(
        string id,
        string? note)
    {
        if (note?.Length > 500)
        {
            TempData["ErrorMessage"] =
                "Catatan maksimal 500 karakter.";

            return RedirectToAction(nameof(Index));
        }

        var admin = await _userManager.GetUserAsync(User);

        if (admin == null)
        {
            return Challenge();
        }

        var user = await _userManager.FindByIdAsync(id);

        if (user == null)
        {
            TempData["ErrorMessage"] =
                "Pengguna tidak ditemukan.";

            return RedirectToAction(nameof(Index));
        }

        if (user.Id == admin.Id)
        {
            TempData["ErrorMessage"] =
                "Admin tidak dapat menolak pendaftarannya sendiri.";

            return RedirectToAction(nameof(Index));
        }

        if (user.ApprovalStatus != AccountApprovalStatus.Pending)
        {
            TempData["ErrorMessage"] =
                "Akun ini sudah diproses sebelumnya.";

            return RedirectToAction(nameof(Index));
        }

        user.ApprovalStatus = AccountApprovalStatus.Rejected;
        user.IsActive = false;
        user.ApprovalReviewedAtUtc = DateTime.UtcNow;
        user.ApprovalReviewedByUserId = admin.Id;
        user.ApprovalNote = string.IsNullOrWhiteSpace(note)
            ? "Pendaftaran ditolak oleh Admin."
            : note.Trim();

        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] =
                "Gagal menyimpan keputusan penolakan.";

            return RedirectToAction(nameof(Index));
        }

        // ==========================================
        // RECORD AUDIT LOG
        // ==========================================

        await _auditLogService.LogAsync(
            admin.Id,
            "UserRejected",
            "User",
            user.Id,
            $"Admin rejected registration for {user.FullName} " +
            $"({user.Email}). Reason: {user.ApprovalNote}",
            HttpContext.Connection.RemoteIpAddress?.ToString()
        );

        TempData["SuccessMessage"] =
            $"Pendaftaran {user.FullName} berhasil ditolak.";

        return RedirectToAction(nameof(Index));
    }
}