
using System.Security.Claims;
using ITHelpdesk.Web.DTOs.Tickets;
using ITHelpdesk.Web.Models.Enums;
using ITHelpdesk.Web.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ITHelpdesk.Web.Controllers;

[Authorize(Roles = "Admin,IT Support,Employee")]
public class TicketsController : Controller
{
    private readonly ITicketService _ticketService;

    public TicketsController(ITicketService ticketService)
    {
        _ticketService = ticketService;
    }

    // ==========================================
    // USER ROLE HELPERS
    // ==========================================

    private bool IsAdmin =>
        User.IsInRole("Admin");

    private bool IsITSupportOnly =>
        User.IsInRole("IT Support") && !IsAdmin;

    private bool IsEmployeeOnly =>
        User.IsInRole("Employee") &&
        !IsAdmin &&
        !User.IsInRole("IT Support");

    private bool CanManageAllTickets =>
        IsAdmin || IsITSupportOnly;

    private string? GetCurrentUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier);
    }

    private string? GetCreatedByFilter()
    {
        return IsEmployeeOnly
            ? GetCurrentUserId()
            : null;
    }

    private string? GetAssignedToFilter()
    {
        return IsITSupportOnly
            ? GetCurrentUserId()
            : null;
    }

    private string? GetClientIpAddress()
    {
        return HttpContext.Connection.RemoteIpAddress?.ToString();
    }

    // ==========================================
    // GET: /Tickets
    // ==========================================

    public async Task<IActionResult> Index(
        string? search,
        TicketStatus? status,
        TicketPriority? priority)
    {
        if (string.IsNullOrWhiteSpace(GetCurrentUserId()))
        {
            return Challenge();
        }

        var tickets = await _ticketService.GetAllAsync(
            search,
            status,
            priority,
            GetCreatedByFilter(),
            GetAssignedToFilter()
        );

        ViewBag.Search = search;

        ViewBag.StatusOptions = Enum.GetValues<TicketStatus>()
            .Select(s => new SelectListItem
            {
                Text = s.ToString(),
                Value = s.ToString(),
                Selected = status == s
            });

        ViewBag.PriorityOptions = Enum.GetValues<TicketPriority>()
            .Select(p => new SelectListItem
            {
                Text = p.ToString(),
                Value = p.ToString(),
                Selected = priority == p
            });

        ViewBag.IsEmployee = IsEmployeeOnly;
        ViewBag.IsITSupport = IsITSupportOnly;
        ViewBag.IsAdmin = IsAdmin;

        return View(tickets);
    }

    // ==========================================
    // GET: /Tickets/Details/1
    // ==========================================

    public async Task<IActionResult> Details(int id)
    {
        var ticket = await _ticketService.GetByIdAsync(
            id,
            GetCreatedByFilter(),
            GetAssignedToFilter()
        );

        if (ticket == null)
        {
            return NotFound();
        }

        if (IsEmployeeOnly)
        {
            ticket.Comments = ticket.Comments
                .Where(c => !c.IsInternal)
                .ToList();
        }

        ViewBag.IsEmployee = IsEmployeeOnly;
        ViewBag.IsITSupport = IsITSupportOnly;
        ViewBag.IsAdmin = IsAdmin;
        ViewBag.CanManageAllTickets = CanManageAllTickets;

        return View(ticket);
    }

    // ==========================================
    // GET: /Tickets/Create
    // ==========================================

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await PopulateCategoriesAsync();

        return View(new CreateTicketDto());
    }

    // ==========================================
    // POST: /Tickets/Create
    // ==========================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateTicketDto dto)
    {
        if (!ModelState.IsValid)
        {
            await PopulateCategoriesAsync(dto.CategoryId);
            return View(dto);
        }

        var creatorId = GetCurrentUserId();

        if (string.IsNullOrWhiteSpace(creatorId))
        {
            return Challenge();
        }

        var ticketId = await _ticketService.CreateAsync(
            dto,
            creatorId,
            GetClientIpAddress()
        );

        TempData["Success"] = "Ticket created successfully.";

        return RedirectToAction(
            nameof(Details),
            new { id = ticketId }
        );
    }

    // ==========================================
    // GET: /Tickets/Edit/1
    // ==========================================

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var ticket = await _ticketService.GetByIdAsync(
            id,
            GetCreatedByFilter(),
            GetAssignedToFilter()
        );

        if (ticket == null)
        {
            return NotFound();
        }

        if (IsEmployeeOnly && ticket.Status != TicketStatus.Open)
        {
            return Forbid();
        }

        var dto = new UpdateTicketDto
        {
            Title = ticket.Title,
            Description = ticket.Description,
            CategoryId = ticket.CategoryId,
            Priority = ticket.Priority
        };

        await PopulateCategoriesAsync(dto.CategoryId);

        return View(dto);
    }

    // ==========================================
    // POST: /Tickets/Edit/1
    // ==========================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        UpdateTicketDto dto)
    {
        var ticket = await _ticketService.GetByIdAsync(
            id,
            GetCreatedByFilter(),
            GetAssignedToFilter()
        );

        if (ticket == null)
        {
            return NotFound();
        }

        if (IsEmployeeOnly && ticket.Status != TicketStatus.Open)
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            await PopulateCategoriesAsync(dto.CategoryId);
            return View(dto);
        }

        var updatedByUserId = GetCurrentUserId();

        if (string.IsNullOrWhiteSpace(updatedByUserId))
        {
            return Challenge();
        }

        var updated = await _ticketService.UpdateAsync(
            id,
            dto,
            updatedByUserId,
            GetClientIpAddress()
        );

        if (!updated)
        {
            return NotFound();
        }

        TempData["Success"] = "Ticket updated successfully.";

        return RedirectToAction(
            nameof(Details),
            new { id }
        );
    }

    // ==========================================
    // POST: /Tickets/AddComment/1
    // ==========================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddComment(
        int id,
        CreateTicketCommentDto dto)
    {
        var userId = GetCurrentUserId();

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        if (!ModelState.IsValid)
        {
            TempData["Error"] =
                "Please enter a valid comment. Maximum length is 2000 characters.";

            return RedirectToAction(
                nameof(Details),
                new { id }
            );
        }

        try
        {
            var added = await _ticketService.AddCommentAsync(
                id,
                dto,
                userId,
                IsAdmin,
                User.IsInRole("IT Support"),
                GetClientIpAddress()
            );

            if (!added)
            {
                return NotFound();
            }

            TempData["Success"] = dto.IsInternal
                ? "Internal note added successfully."
                : "Comment added successfully.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(
            nameof(Details),
            new { id }
        );
    }

    // ==========================================
    // POST: /Tickets/UpdateStatus/1
    // ==========================================

    [Authorize(Roles = "Admin,IT Support")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(
        int id,
        TicketStatus newStatus,
        string? note)
    {
        if (!Enum.IsDefined(newStatus))
        {
            TempData["Error"] = "Invalid ticket status.";

            return RedirectToAction(nameof(Index));
        }

        if (note?.Length > 1000)
        {
            TempData["Error"] =
                "Status note cannot exceed 1000 characters.";

            return RedirectToAction(
                nameof(Details),
                new { id }
            );
        }

        var ticket = await _ticketService.GetByIdAsync(
            id,
            assignedToUserId: GetAssignedToFilter()
        );

        if (ticket == null)
        {
            return NotFound();
        }

        var changedByUserId = GetCurrentUserId();

        if (string.IsNullOrWhiteSpace(changedByUserId))
        {
            return Challenge();
        }

        try
        {
            var updated = await _ticketService.UpdateStatusAsync(
                id,
                newStatus,
                changedByUserId,
                note,
                GetClientIpAddress()
            );

            if (!updated)
            {
                return NotFound();
            }

            TempData["Success"] =
                $"Ticket status successfully changed to {newStatus}.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(
            nameof(Details),
            new { id }
        );
    }

    // ==========================================
    // POST: /Tickets/Delete/1
    // ==========================================

    [Authorize(Roles = "Admin,IT Support")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var ticket = await _ticketService.GetByIdAsync(
            id,
            assignedToUserId: GetAssignedToFilter()
        );

        if (ticket == null)
        {
            return NotFound();
        }

        var deletedByUserId = GetCurrentUserId();

        if (string.IsNullOrWhiteSpace(deletedByUserId))
        {
            return Challenge();
        }

        var deleted = await _ticketService.SoftDeleteAsync(
            id,
            deletedByUserId,
            GetClientIpAddress()
        );

        if (!deleted)
        {
            return NotFound();
        }

        TempData["Success"] = "Ticket deleted successfully.";

        return RedirectToAction(nameof(Index));
    }

    // ==========================================
    // LOAD CATEGORIES FOR DROPDOWN
    // ==========================================

    private async Task PopulateCategoriesAsync(
        int? selectedCategoryId = null)
    {
        var categories = await _ticketService.GetCategoriesAsync();

        ViewBag.CategoryOptions = new SelectList(
            categories,
            "Id",
            "Name",
            selectedCategoryId
        );
    }
}
