
using ITHelpdesk.Web.DTOs.Tickets;
using ITHelpdesk.Web.Models.Enums;

namespace ITHelpdesk.Web.Services.Interfaces;

public interface ITicketService
{
    // ==========================================
    // RETRIEVE TICKETS
    // ==========================================

    Task<IReadOnlyList<TicketListItemDto>> GetAllAsync(
        string? search = null,
        TicketStatus? status = null,
        TicketPriority? priority = null,
        string? createdByUserId = null,
        string? assignedToUserId = null
    );

    Task<TicketDetailDto?> GetByIdAsync(
        int id,
        string? createdByUserId = null,
        string? assignedToUserId = null
    );

    // ==========================================
    // TICKET CATEGORIES
    // ==========================================

    Task<IReadOnlyList<TicketCategoryDto>> GetCategoriesAsync();

    // ==========================================
    // CREATE TICKET
    // ==========================================

    Task<int> CreateAsync(
        CreateTicketDto dto,
        string createdByUserId,
        string? ipAddress = null
    );

    // ==========================================
    // UPDATE TICKET
    // ==========================================

    Task<bool> UpdateAsync(
        int id,
        UpdateTicketDto dto,
        string updatedByUserId,
        string? ipAddress = null
    );

    // ==========================================
    // ADD TICKET COMMENT
    // ==========================================

    Task<bool> AddCommentAsync(
        int ticketId,
        CreateTicketCommentDto dto,
        string userId,
        bool isAdmin,
        bool isITSupport,
        string? ipAddress = null
    );

    // ==========================================
    // UPDATE TICKET STATUS
    // ==========================================

    Task<bool> UpdateStatusAsync(
        int id,
        TicketStatus newStatus,
        string changedByUserId,
        string? note,
        string? ipAddress = null
    );

    // ==========================================
    // SOFT DELETE TICKET
    // ==========================================

    Task<bool> SoftDeleteAsync(
        int id,
        string deletedByUserId,
        string? ipAddress = null
    );
}
