
using System.Security.Claims;
using ITHelpdesk.Web.DTOs.Tickets;
using ITHelpdesk.Web.Models.Enums;
using ITHelpdesk.Web.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ITHelpdesk.Web.Controllers.Api;

[ApiController]
[Authorize(Roles = "Admin,IT Support,Employee")]
[Route("api/tickets")]
public class TicketsApiController : ControllerBase
{
    private readonly ITicketService _ticketService;

    public TicketsApiController(ITicketService ticketService)
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

    private string? GetCurrentUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier);
    }

    // Employee can only access tickets they created.
    private string? GetCreatedByFilter()
    {
        return IsEmployeeOnly
            ? GetCurrentUserId()
            : null;
    }

    // IT Support can only access tickets assigned to them.
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
    // GET: api/tickets
    // Retrieve tickets with optional filters
    // ==========================================

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search,
        [FromQuery] TicketStatus? status,
        [FromQuery] TicketPriority? priority)
    {
        if (string.IsNullOrWhiteSpace(GetCurrentUserId()))
        {
            return Unauthorized();
        }

        var tickets = await _ticketService.GetAllAsync(
            search,
            status,
            priority,
            GetCreatedByFilter(),
            GetAssignedToFilter()
        );

        return Ok(tickets);
    }

    // ==========================================
    // GET: api/tickets/1
    // Retrieve ticket detail
    // ==========================================

    [HttpGet("{id:int}", Name = "GetTicketById")]
    public async Task<IActionResult> GetById(int id)
    {
        if (string.IsNullOrWhiteSpace(GetCurrentUserId()))
        {
            return Unauthorized();
        }

        var ticket = await _ticketService.GetByIdAsync(
            id,
            GetCreatedByFilter(),
            GetAssignedToFilter()
        );

        if (ticket == null)
        {
            return NotFound(new
            {
                message = "Ticket not found."
            });
        }

        // Employee cannot see internal comments.
        if (IsEmployeeOnly)
        {
            ticket.Comments = ticket.Comments
                .Where(c => !c.IsInternal)
                .ToList();
        }

        return Ok(ticket);
    }

    // ==========================================
    // GET: api/tickets/1/comments
    // Retrieve comments for an accessible ticket
    // ==========================================

    [HttpGet("{id:int}/comments")]
    public async Task<IActionResult> GetComments(int id)
    {
        if (id <= 0)
        {
            return BadRequest(new
            {
                message = "Ticket ID must be greater than zero."
            });
        }

        if (string.IsNullOrWhiteSpace(GetCurrentUserId()))
        {
            return Unauthorized();
        }

        var ticket = await _ticketService.GetByIdAsync(
            id,
            GetCreatedByFilter(),
            GetAssignedToFilter()
        );

        if (ticket == null)
        {
            return NotFound(new
            {
                message = "Ticket not found."
            });
        }

        var comments = IsEmployeeOnly
            ? ticket.Comments.Where(comment => !comment.IsInternal).ToList()
            : ticket.Comments;

        return Ok(comments);
    }

    // ==========================================
    // POST: api/tickets/1/comments
    // Add a public comment or authorized internal note
    // ==========================================

    [HttpPost("{id:int}/comments")]
    public async Task<IActionResult> AddComment(
        int id,
        [FromBody] CreateTicketCommentDto dto)
    {
        if (id <= 0)
        {
            return BadRequest(new
            {
                message = "Ticket ID must be greater than zero."
            });
        }

        var userId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
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
                return NotFound(new
                {
                    message = "Ticket not found or you do not have access to it."
                });
            }

            return NoContent();
        }
        catch (InvalidOperationException ex)
            when (ex.Message == "You are not authorized to create internal notes."
                || ex.Message == "User was not found or is not authorized.")
        {
            return StatusCode(
                StatusCodes.Status403Forbidden,
                new { message = ex.Message }
            );
        }
        catch (InvalidOperationException ex)
            when (ex.Message == "Comments cannot be added to a closed ticket.")
        {
            return Conflict(new
            {
                message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }

    // ==========================================
    // POST: api/tickets
    // Create a new ticket
    // ==========================================

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateTicketDto dto)
    {
        var userId = GetCurrentUserId();

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        try
        {
            var ticketId = await _ticketService.CreateAsync(
                dto,
                userId,
                GetClientIpAddress()
            );

            // Retrieve the ticket created by this user.
            var createdTicket = await _ticketService.GetByIdAsync(
                ticketId,
                createdByUserId: userId
            );

            if (createdTicket == null)
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new
                    {
                        message = "Ticket was created but could not be retrieved."
                    }
                );
            }

            return CreatedAtRoute(
                "GetTicketById",
                new { id = ticketId },
                createdTicket
            );
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }

    // ==========================================
    // PUT: api/tickets/1
    // Update ticket information
    // ==========================================

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdateTicketDto dto)
    {
        // Check ownership or assignment.
        var ticket = await _ticketService.GetByIdAsync(
            id,
            GetCreatedByFilter(),
            GetAssignedToFilter()
        );

        if (ticket == null)
        {
            return NotFound(new
            {
                message = "Ticket not found."
            });
        }

        // Employee can only edit tickets with Open status.
        if (IsEmployeeOnly && ticket.Status != TicketStatus.Open)
        {
            return StatusCode(
                StatusCodes.Status403Forbidden,
                new
                {
                    message = "You can only edit tickets with Open status."
                }
            );
        }

        var userId = GetCurrentUserId();

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        try
        {
            var updated = await _ticketService.UpdateAsync(
                id,
                dto,
                userId,
                GetClientIpAddress()
            );

            if (!updated)
            {
                return NotFound(new
                {
                    message = "Ticket not found."
                });
            }

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }

    // ==========================================
    // PATCH: api/tickets/1/status
    // Update ticket status
    // ==========================================

    [Authorize(Roles = "Admin,IT Support")]
    [HttpPatch("{id:int}/status")]
    public async Task<IActionResult> UpdateStatus(
        int id,
        [FromBody] UpdateTicketStatusDto dto)
    {
        var userId = GetCurrentUserId();

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        // IT Support must own the assignment.
        // Admin can access every ticket.
        var ticket = await _ticketService.GetByIdAsync(
            id,
            assignedToUserId: GetAssignedToFilter()
        );

        if (ticket == null)
        {
            return NotFound(new
            {
                message = "Ticket not found or not assigned to you."
            });
        }

        try
        {
            var updated = await _ticketService.UpdateStatusAsync(
                id,
                dto.Status,
                userId,
                dto.Note,
                GetClientIpAddress()
            );

            if (!updated)
            {
                return NotFound(new
                {
                    message = "Ticket not found."
                });
            }

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }

    // ==========================================
    // DELETE: api/tickets/1
    // Soft delete ticket
    // ==========================================

    [Authorize(Roles = "Admin,IT Support")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = GetCurrentUserId();

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        // IT Support can only delete assigned tickets.
        // Admin can delete any ticket.
        var ticket = await _ticketService.GetByIdAsync(
            id,
            assignedToUserId: GetAssignedToFilter()
        );

        if (ticket == null)
        {
            return NotFound(new
            {
                message = "Ticket not found or not assigned to you."
            });
        }

        try
        {
            var deleted = await _ticketService.SoftDeleteAsync(
                id,
                userId,
                GetClientIpAddress()
            );

            if (!deleted)
            {
                return NotFound(new
                {
                    message = "Ticket not found."
                });
            }

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }
}
