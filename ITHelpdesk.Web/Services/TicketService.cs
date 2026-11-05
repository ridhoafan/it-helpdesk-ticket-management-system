
using ITHelpdesk.Web.Data;
using ITHelpdesk.Web.DTOs.Tickets;
using ITHelpdesk.Web.Models.Entities;
using ITHelpdesk.Web.Models.Enums;
using ITHelpdesk.Web.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ITHelpdesk.Web.Services;

public class TicketService : ITicketService
{
    private readonly AppDbContext _context;
    private readonly IAuditLogService _auditLogService;

    public TicketService(
        AppDbContext context,
        IAuditLogService auditLogService)
    {
        _context = context;
        _auditLogService = auditLogService;
    }

    // ==========================================
    // RETRIEVE ALL TICKETS
    // ==========================================

    public async Task<IReadOnlyList<TicketListItemDto>> GetAllAsync(
        string? search = null,
        TicketStatus? status = null,
        TicketPriority? priority = null,
        string? createdByUserId = null,
        string? assignedToUserId = null)
    {
        var query = _context.Tickets
            .AsNoTracking()
            .Include(t => t.Category)
            .Include(t => t.CreatedByUser)
            .Include(t => t.AssignedToUser)
            .Where(t => !t.IsDeleted);

        if (!string.IsNullOrWhiteSpace(createdByUserId))
        {
            query = query.Where(t =>
                t.CreatedByUserId == createdByUserId);
        }

        if (!string.IsNullOrWhiteSpace(assignedToUserId))
        {
            query = query.Where(t =>
                t.AssignedToUserId == assignedToUserId);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();

            query = query.Where(t =>
                t.Title.Contains(search) ||
                t.TicketNumber.Contains(search));
        }

        if (status.HasValue)
        {
            query = query.Where(t =>
                t.Status == status.Value);
        }

        if (priority.HasValue)
        {
            query = query.Where(t =>
                t.Priority == priority.Value);
        }

        return await query
            .OrderByDescending(t => t.CreatedAtUtc)
            .Select(t => new TicketListItemDto
            {
                Id = t.Id,
                TicketNumber = t.TicketNumber,
                Title = t.Title,
                CategoryName = t.Category.Name,
                Priority = t.Priority,
                Status = t.Status,
                CreatedByName = t.CreatedByUser.FullName,
                AssignedToName = t.AssignedToUser != null
                    ? t.AssignedToUser.FullName
                    : null,
                CreatedAtUtc = t.CreatedAtUtc
            })
            .ToListAsync();
    }

    // ==========================================
    // RETRIEVE TICKET DETAIL
    // ==========================================

    public async Task<TicketDetailDto?> GetByIdAsync(
        int id,
        string? createdByUserId = null,
        string? assignedToUserId = null)
    {
        var query = _context.Tickets
            .AsNoTracking()
            .Include(t => t.Category)
            .Include(t => t.CreatedByUser)
            .Include(t => t.AssignedToUser)
            .Include(t => t.Comments)
                .ThenInclude(c => c.User)
            .Include(t => t.StatusHistories)
                .ThenInclude(h => h.ChangedByUser)
            .Where(t => t.Id == id && !t.IsDeleted);

        if (!string.IsNullOrWhiteSpace(createdByUserId))
        {
            query = query.Where(t =>
                t.CreatedByUserId == createdByUserId);
        }

        if (!string.IsNullOrWhiteSpace(assignedToUserId))
        {
            query = query.Where(t =>
                t.AssignedToUserId == assignedToUserId);
        }

        var ticket = await query.FirstOrDefaultAsync();

        if (ticket == null)
        {
            return null;
        }

        return new TicketDetailDto
        {
            Id = ticket.Id,
            TicketNumber = ticket.TicketNumber,
            Title = ticket.Title,
            Description = ticket.Description,
            CategoryId = ticket.CategoryId,
            CategoryName = ticket.Category.Name,
            Priority = ticket.Priority,
            Status = ticket.Status,
            CreatedByName = ticket.CreatedByUser.FullName,
            AssignedToName = ticket.AssignedToUser?.FullName,
            CreatedAtUtc = ticket.CreatedAtUtc,

            Comments = ticket.Comments
                .OrderBy(c => c.CreatedAtUtc)
                .Select(c => new TicketCommentDto
                {
                    UserName = c.User.FullName,
                    Content = c.Content,
                    IsInternal = c.IsInternal,
                    CreatedAtUtc = c.CreatedAtUtc
                })
                .ToList(),

            StatusHistories = ticket.StatusHistories
                .OrderBy(h => h.ChangedAtUtc)
                .Select(h => new TicketStatusHistoryDto
                {
                    OldStatus = h.OldStatus,
                    NewStatus = h.NewStatus,
                    ChangedByName = h.ChangedByUser.FullName,
                    Note = h.Note,
                    ChangedAtUtc = h.ChangedAtUtc
                })
                .ToList()
        };
    }

    // ==========================================
    // RETRIEVE ACTIVE CATEGORIES
    // ==========================================

    public async Task<IReadOnlyList<TicketCategoryDto>>
        GetCategoriesAsync()
    {
        return await _context.TicketCategories
            .AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .Select(c => new TicketCategoryDto
            {
                Id = c.Id,
                Name = c.Name
            })
            .ToListAsync();
    }

    // ==========================================
    // FIND IT SUPPORT WITH LOWEST WORKLOAD
    // ==========================================

    private async Task<ApplicationUser?> FindLeastLoadedSupportAsync(
        int categoryId,
        int? excludedTicketId = null)
    {
        var candidates = await (
            from specialization in _context.ITSupportCategories.AsNoTracking()

            join support in _context.Users.AsNoTracking()
                on specialization.ITSupportUserId equals support.Id

            join userRole in _context.UserRoles.AsNoTracking()
                on support.Id equals userRole.UserId

            join role in _context.Roles.AsNoTracking()
                on userRole.RoleId equals role.Id

            where specialization.TicketCategoryId == categoryId
                && support.IsActive
                && support.ApprovalStatus == AccountApprovalStatus.Approved
                && role.Name == "IT Support"

            select support
        )
        .Distinct()
        .ToListAsync();

        if (candidates.Count == 0)
        {
            return null;
        }

        var candidateIds = candidates
            .Select(c => c.Id)
            .ToList();

        var workloadQuery = _context.Tickets
            .AsNoTracking()
            .Where(t =>
                !t.IsDeleted &&
                t.AssignedToUserId != null &&
                candidateIds.Contains(t.AssignedToUserId) &&
                (t.Status == TicketStatus.Open ||
                 t.Status == TicketStatus.InProgress));

        if (excludedTicketId.HasValue)
        {
            workloadQuery = workloadQuery.Where(t =>
                t.Id != excludedTicketId.Value);
        }

        var workloads = await workloadQuery
            .GroupBy(t => t.AssignedToUserId!)
            .Select(g => new
            {
                UserId = g.Key,
                ActiveTicketCount = g.Count()
            })
            .ToDictionaryAsync(
                x => x.UserId,
                x => x.ActiveTicketCount
            );

        return candidates
            .OrderBy(candidate =>
                workloads.ContainsKey(candidate.Id)
                    ? workloads[candidate.Id]
                    : 0)
            .ThenBy(candidate => candidate.Id)
            .FirstOrDefault();
    }

    // ==========================================
    // CREATE TICKET
    // ==========================================

    public async Task<int> CreateAsync(
        CreateTicketDto dto,
        string createdByUserId,
        string? ipAddress = null)
    {
        if (!Enum.IsDefined(dto.Priority))
        {
            throw new InvalidOperationException(
                "Ticket priority is invalid."
            );
        }

        var category = await _context.TicketCategories
            .AsNoTracking()
            .FirstOrDefaultAsync(c =>
                c.Id == dto.CategoryId && c.IsActive);

        if (category == null)
        {
            throw new InvalidOperationException(
                "Selected category does not exist or is inactive."
            );
        }

        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u =>
                u.Id == createdByUserId &&
                u.IsActive &&
                u.ApprovalStatus == AccountApprovalStatus.Approved);

        if (user == null)
        {
            throw new InvalidOperationException(
                "Ticket creator was not found or is not authorized."
            );
        }

        var assignedSupport = await FindLeastLoadedSupportAsync(
            dto.CategoryId
        );

        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        try
        {
            var ticket = new Ticket
            {
                TicketNumber =
                    "TCK-" +
                    Guid.NewGuid().ToString("N")[..20]
                        .ToUpperInvariant(),

                Title = dto.Title.Trim(),
                Description = dto.Description.Trim(),
                CategoryId = dto.CategoryId,
                CreatedByUserId = createdByUserId,
                AssignedToUserId = assignedSupport?.Id,
                Priority = dto.Priority,
                Status = TicketStatus.Open,
                CreatedAtUtc = DateTime.UtcNow
            };

            _context.Tickets.Add(ticket);

            await _context.SaveChangesAsync();

            var historyNote = assignedSupport == null
                ? "Ticket created. No matching IT Support is available; awaiting Admin assignment."
                : $"Ticket created and automatically assigned to {assignedSupport.FullName}.";

            var history = new TicketStatusHistory
            {
                TicketId = ticket.Id,
                OldStatus = null,
                NewStatus = TicketStatus.Open,
                ChangedByUserId = createdByUserId,
                Note = historyNote,
                ChangedAtUtc = DateTime.UtcNow
            };

            _context.TicketStatusHistories.Add(history);

            var creationDescription =
                $"User {user.FullName} created ticket " +
                $"{ticket.TicketNumber}: {ticket.Title}. " +
                $"Category: {category.Name}.";

            if (assignedSupport != null)
            {
                creationDescription +=
                    $" Automatically assigned to {assignedSupport.FullName}.";
            }
            else
            {
                creationDescription +=
                    " No eligible IT Support was found. " +
                    "Ticket remains unassigned for Admin review.";
            }

            await _auditLogService.LogAsync(
                createdByUserId,
                "TicketCreated",
                "Ticket",
                ticket.Id.ToString(),
                creationDescription,
                ipAddress
            );

            if (assignedSupport != null)
            {
                await _auditLogService.LogAsync(
                    createdByUserId,
                    "TicketAutoAssigned",
                    "Ticket",
                    ticket.Id.ToString(),
                    $"System automatically assigned ticket " +
                    $"{ticket.TicketNumber} to {assignedSupport.FullName} " +
                    $"based on {category.Name} specialization and workload.",
                    ipAddress
                );
            }
            else
            {
                await _auditLogService.LogAsync(
                    createdByUserId,
                    "TicketAssignmentPending",
                    "Ticket",
                    ticket.Id.ToString(),
                    $"Ticket {ticket.TicketNumber} in category " +
                    $"{category.Name} has no eligible IT Support. " +
                    "Admin review is required.",
                    ipAddress
                );
            }

            await transaction.CommitAsync();

            return ticket.Id;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    // ==========================================
    // UPDATE TICKET INFORMATION
    // ==========================================

    public async Task<bool> UpdateAsync(
        int id,
        UpdateTicketDto dto,
        string updatedByUserId,
        string? ipAddress = null)
    {
        if (!Enum.IsDefined(dto.Priority))
        {
            throw new InvalidOperationException(
                "Ticket priority is invalid."
            );
        }

        var userExists = await _context.Users.AnyAsync(u =>
            u.Id == updatedByUserId &&
            u.IsActive &&
            u.ApprovalStatus == AccountApprovalStatus.Approved);

        if (!userExists)
        {
            throw new InvalidOperationException(
                "User performing this action was not found or is not authorized."
            );
        }

        var ticket = await _context.Tickets
            .FirstOrDefaultAsync(t =>
                t.Id == id && !t.IsDeleted);

        if (ticket == null)
        {
            return false;
        }

        var categoryExists = await _context.TicketCategories
            .AnyAsync(c =>
                c.Id == dto.CategoryId && c.IsActive);

        if (!categoryExists)
        {
            throw new InvalidOperationException(
                "Selected category does not exist or is inactive."
            );
        }

        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        try
        {
            var oldTitle = ticket.Title;
            var oldCategoryId = ticket.CategoryId;
            var ticketNumber = ticket.TicketNumber;

            ticket.Title = dto.Title.Trim();
            ticket.Description = dto.Description.Trim();
            ticket.CategoryId = dto.CategoryId;
            ticket.Priority = dto.Priority;
            ticket.UpdatedAtUtc = DateTime.UtcNow;

            if (oldCategoryId != dto.CategoryId)
            {
                var assignedSupport = await FindLeastLoadedSupportAsync(
                    dto.CategoryId,
                    ticket.Id
                );

                ticket.AssignedToUserId = assignedSupport?.Id;

                var assignmentDescription = assignedSupport == null
                    ? $"Ticket {ticketNumber} category changed. " +
                      "No matching IT Support is available; " +
                      "ticket is now unassigned for Admin review."
                    : $"Ticket {ticketNumber} category changed and " +
                      $"automatically assigned to {assignedSupport.FullName}.";

                await _auditLogService.LogAsync(
                    updatedByUserId,
                    "TicketAssignmentUpdated",
                    "Ticket",
                    ticket.Id.ToString(),
                    assignmentDescription,
                    ipAddress
                );
            }

            await _auditLogService.LogAsync(
                updatedByUserId,
                "TicketUpdated",
                "Ticket",
                ticket.Id.ToString(),
                $"Ticket {ticketNumber} was updated. " +
                $"Previous title: {oldTitle}. " +
                $"New title: {ticket.Title}.",
                ipAddress
            );

            await transaction.CommitAsync();

            return true;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    // ==========================================
    // ADD TICKET COMMENT
    // ==========================================

    public async Task<bool> AddCommentAsync(
        int ticketId,
        CreateTicketCommentDto dto,
        string userId,
        bool isAdmin,
        bool isITSupport,
        string? ipAddress = null)
    {
        var content = dto.Content?.Trim();

        if (string.IsNullOrWhiteSpace(content))
        {
            throw new InvalidOperationException(
                "Comment cannot be empty."
            );
        }

        if (content.Length > 2000)
        {
            throw new InvalidOperationException(
                "Comment cannot exceed 2000 characters."
            );
        }

        if (dto.IsInternal && !isAdmin && !isITSupport)
        {
            throw new InvalidOperationException(
                "You are not authorized to create internal notes."
            );
        }

        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u =>
                u.Id == userId &&
                u.IsActive &&
                u.ApprovalStatus == AccountApprovalStatus.Approved);

        if (user == null)
        {
            throw new InvalidOperationException(
                "User was not found or is not authorized."
            );
        }

        var ticket = await _context.Tickets
            .FirstOrDefaultAsync(t =>
                t.Id == ticketId && !t.IsDeleted);

        if (ticket == null)
        {
            return false;
        }

        if (!isAdmin && isITSupport)
        {
            if (ticket.AssignedToUserId != userId)
            {
                return false;
            }
        }

        if (!isAdmin && !isITSupport)
        {
            if (ticket.CreatedByUserId != userId)
            {
                return false;
            }
        }

        if (ticket.Status == TicketStatus.Closed)
        {
            throw new InvalidOperationException(
                "Comments cannot be added to a closed ticket."
            );
        }

        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        try
        {
            var comment = new TicketComment
            {
                TicketId = ticket.Id,
                UserId = userId,
                Content = content,
                IsInternal = dto.IsInternal,
                CreatedAtUtc = DateTime.UtcNow
            };

            _context.TicketComments.Add(comment);

            var commentType = dto.IsInternal
                ? "internal note"
                : "public comment";

            await _auditLogService.LogAsync(
                userId,
                "TicketCommentAdded",
                "Ticket",
                ticket.Id.ToString(),
                $"{user.FullName} added a {commentType} " +
                $"to ticket {ticket.TicketNumber}.",
                ipAddress
            );

            await transaction.CommitAsync();

            return true;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    // ==========================================
    // UPDATE TICKET STATUS
    // ==========================================

    public async Task<bool> UpdateStatusAsync(
        int id,
        TicketStatus newStatus,
        string changedByUserId,
        string? note,
        string? ipAddress = null)
    {
        if (!Enum.IsDefined(newStatus))
        {
            throw new InvalidOperationException(
                "Invalid ticket status."
            );
        }

        // Resolution requires a public explanation.
        if (newStatus == TicketStatus.Resolved &&
            string.IsNullOrWhiteSpace(note))
        {
            throw new InvalidOperationException(
                "Resolution summary is required before resolving this ticket."
            );
        }

        if (note?.Length > 1000)
        {
            throw new InvalidOperationException(
                "Status note cannot exceed 1000 characters."
            );
        }

        var ticket = await _context.Tickets
            .FirstOrDefaultAsync(t =>
                t.Id == id && !t.IsDeleted);

        if (ticket == null)
        {
            return false;
        }

        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u =>
                u.Id == changedByUserId &&
                u.IsActive &&
                u.ApprovalStatus == AccountApprovalStatus.Approved);

        if (user == null)
        {
            throw new InvalidOperationException(
                "User performing this action was not found or is not authorized."
            );
        }

        bool isValidTransition = (ticket.Status, newStatus) switch
        {
            (TicketStatus.Open, TicketStatus.InProgress) => true,
            (TicketStatus.InProgress, TicketStatus.Resolved) => true,
            (TicketStatus.Resolved, TicketStatus.Closed) => true,
            _ => false
        };

        if (!isValidTransition)
        {
            throw new InvalidOperationException(
                $"Cannot change status from {ticket.Status} to {newStatus}."
            );
        }

        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        try
        {
            var oldStatus = ticket.Status;
            var ticketNumber = ticket.TicketNumber;
            var currentTime = DateTime.UtcNow;

            ticket.Status = newStatus;
            ticket.UpdatedAtUtc = currentTime;

            // Record status history.
            var history = new TicketStatusHistory
            {
                TicketId = ticket.Id,
                OldStatus = oldStatus,
                NewStatus = newStatus,
                ChangedByUserId = changedByUserId,
                Note = string.IsNullOrWhiteSpace(note)
                    ? null
                    : note.Trim(),
                ChangedAtUtc = currentTime
            };

            _context.TicketStatusHistories.Add(history);

            // ======================================
            // AUTOMATIC PUBLIC RESOLUTION COMMENT
            // ======================================

            if (newStatus == TicketStatus.Resolved)
            {
                var resolutionComment = new TicketComment
                {
                    TicketId = ticket.Id,
                    UserId = changedByUserId,
                    Content = note!.Trim(),
                    IsInternal = false,
                    CreatedAtUtc = currentTime
                };

                _context.TicketComments.Add(resolutionComment);

                await _auditLogService.LogAsync(
                    changedByUserId,
                    "TicketCommentAdded",
                    "Ticket",
                    ticket.Id.ToString(),
                    $"{user.FullName} added a public resolution summary " +
                    $"to ticket {ticketNumber}.",
                    ipAddress
                );
            }

            // Record status change in audit logs.
            await _auditLogService.LogAsync(
                changedByUserId,
                "TicketStatusChanged",
                "Ticket",
                ticket.Id.ToString(),
                $"Ticket {ticketNumber} status changed " +
                $"from {oldStatus} to {newStatus} by {user.FullName}.",
                ipAddress
            );

            await transaction.CommitAsync();

            return true;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    // ==========================================
    // SOFT DELETE TICKET
    // ==========================================

    public async Task<bool> SoftDeleteAsync(
        int id,
        string deletedByUserId,
        string? ipAddress = null)
    {
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u =>
                u.Id == deletedByUserId &&
                u.IsActive &&
                u.ApprovalStatus == AccountApprovalStatus.Approved);

        if (user == null)
        {
            throw new InvalidOperationException(
                "User performing this action was not found or is not authorized."
            );
        }

        var ticket = await _context.Tickets
            .FirstOrDefaultAsync(t =>
                t.Id == id && !t.IsDeleted);

        if (ticket == null)
        {
            return false;
        }

        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        try
        {
            var ticketNumber = ticket.TicketNumber;
            var ticketTitle = ticket.Title;

            ticket.IsDeleted = true;
            ticket.UpdatedAtUtc = DateTime.UtcNow;

            await _auditLogService.LogAsync(
                deletedByUserId,
                "TicketDeleted",
                "Ticket",
                ticket.Id.ToString(),
                $"Ticket {ticketNumber} ({ticketTitle}) " +
                $"was soft-deleted by {user.FullName}.",
                ipAddress
            );

            await transaction.CommitAsync();

            return true;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
