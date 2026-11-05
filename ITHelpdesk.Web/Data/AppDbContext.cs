using ITHelpdesk.Web.Models.Entities;
using ITHelpdesk.Web.Models.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ITHelpdesk.Web.Data;

public class AppDbContext
    : IdentityDbContext<ApplicationUser, IdentityRole, string>
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Ticket> Tickets => Set<Ticket>();

    public DbSet<TicketCategory> TicketCategories
        => Set<TicketCategory>();

    public DbSet<TicketComment> TicketComments
        => Set<TicketComment>();

    public DbSet<TicketStatusHistory> TicketStatusHistories
        => Set<TicketStatusHistory>();

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public DbSet<ITSupportCategory> ITSupportCategories
        => Set<ITSupportCategory>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // ==========================================
        // IDENTITY TABLE CONFIGURATION
        // ==========================================

        builder.Entity<ApplicationUser>().ToTable("Users");
        builder.Entity<IdentityRole>().ToTable("Roles");
        builder.Entity<IdentityUserRole<string>>().ToTable("UserRoles");
        builder.Entity<IdentityUserClaim<string>>().ToTable("UserClaims");
        builder.Entity<IdentityUserLogin<string>>().ToTable("UserLogins");
        builder.Entity<IdentityRoleClaim<string>>().ToTable("RoleClaims");
        builder.Entity<IdentityUserToken<string>>().ToTable("UserTokens");

        // ==========================================
        // APPLICATION USER CONFIGURATION
        // ==========================================

        builder.Entity<ApplicationUser>()
            .Property(u => u.FullName)
            .HasMaxLength(150)
            .IsRequired();

        builder.Entity<ApplicationUser>()
            .Property(u => u.ApprovalStatus)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(AccountApprovalStatus.Pending);

        builder.Entity<ApplicationUser>()
            .Property(u => u.ApprovalNote)
            .HasMaxLength(500);

        builder.Entity<ApplicationUser>()
            .HasIndex(u => u.ApprovalStatus);

        builder.Entity<ApplicationUser>()
            .HasOne(u => u.ApprovalReviewedByUser)
            .WithMany()
            .HasForeignKey(u => u.ApprovalReviewedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // ==========================================
        // AUDIT LOG CONFIGURATION
        // ==========================================

        builder.Entity<AuditLog>()
            .Property(a => a.Action)
            .HasMaxLength(50)
            .IsRequired();

        builder.Entity<AuditLog>()
            .Property(a => a.EntityType)
            .HasMaxLength(50)
            .IsRequired();

        builder.Entity<AuditLog>()
            .Property(a => a.EntityId)
            .HasMaxLength(100);

        builder.Entity<AuditLog>()
            .Property(a => a.Description)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Entity<AuditLog>()
            .Property(a => a.IpAddress)
            .HasMaxLength(45);

        builder.Entity<AuditLog>()
            .HasIndex(a => a.CreatedAtUtc);

        builder.Entity<AuditLog>()
            .HasIndex(a => a.UserId);

        builder.Entity<AuditLog>()
            .HasIndex(a => new
            {
                a.EntityType,
                a.EntityId
            });

        builder.Entity<AuditLog>()
            .HasOne(a => a.User)
            .WithMany()
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        // ==========================================
        // TICKET CATEGORY CONFIGURATION
        // ==========================================

        builder.Entity<TicketCategory>()
            .HasIndex(c => c.Name)
            .IsUnique();

        builder.Entity<TicketCategory>()
            .Property(c => c.Name)
            .HasMaxLength(80)
            .IsRequired();

        builder.Entity<TicketCategory>()
            .HasData(
                new TicketCategory
                {
                    Id = 1,
                    Name = "Hardware",
                    Description = "Computer and hardware problems"
                },
                new TicketCategory
                {
                    Id = 2,
                    Name = "Software",
                    Description = "Software and application problems"
                },
                new TicketCategory
                {
                    Id = 3,
                    Name = "Network",
                    Description = "Network and internet problems"
                },
                new TicketCategory
                {
                    Id = 4,
                    Name = "Account & Access",
                    Description = "Account and access problems"
                },
                new TicketCategory
                {
                    Id = 5,
                    Name = "Other",
                    Description = "Other IT-related problems"
                }
            );

        // ==========================================
        // IT SUPPORT SPECIALIZATION CONFIGURATION
        // ==========================================

        builder.Entity<ITSupportCategory>()
            .HasKey(sc => new
            {
                sc.ITSupportUserId,
                sc.TicketCategoryId
            });

        builder.Entity<ITSupportCategory>()
            .HasOne(sc => sc.ITSupportUser)
            .WithMany(u => u.ITSupportCategories)
            .HasForeignKey(sc => sc.ITSupportUserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ITSupportCategory>()
            .HasOne(sc => sc.TicketCategory)
            .WithMany(c => c.ITSupportUsers)
            .HasForeignKey(sc => sc.TicketCategoryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ITSupportCategory>()
            .HasIndex(sc => sc.TicketCategoryId);

        // ==========================================
        // TICKET CONFIGURATION
        // ==========================================

        builder.Entity<Ticket>()
            .HasIndex(t => t.TicketNumber)
            .IsUnique();

        builder.Entity<Ticket>()
            .Property(t => t.TicketNumber)
            .HasMaxLength(30)
            .IsRequired();

        builder.Entity<Ticket>()
            .Property(t => t.Title)
            .HasMaxLength(150)
            .IsRequired();

        builder.Entity<Ticket>()
            .Property(t => t.Description)
            .HasMaxLength(4000)
            .IsRequired();

        builder.Entity<Ticket>()
            .Property(t => t.Priority)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Entity<Ticket>()
            .Property(t => t.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Entity<Ticket>()
            .HasOne(t => t.Category)
            .WithMany(c => c.Tickets)
            .HasForeignKey(t => t.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Ticket>()
            .HasOne(t => t.CreatedByUser)
            .WithMany(u => u.CreatedTickets)
            .HasForeignKey(t => t.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Ticket>()
            .HasOne(t => t.AssignedToUser)
            .WithMany(u => u.AssignedTickets)
            .HasForeignKey(t => t.AssignedToUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // ==========================================
        // TICKET COMMENT CONFIGURATION
        // ==========================================

        builder.Entity<TicketComment>()
            .Property(c => c.Content)
            .HasMaxLength(2000)
            .IsRequired();

        builder.Entity<TicketComment>()
            .HasOne(c => c.Ticket)
            .WithMany(t => t.Comments)
            .HasForeignKey(c => c.TicketId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<TicketComment>()
            .HasOne(c => c.User)
            .WithMany(u => u.TicketComments)
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // ==========================================
        // TICKET STATUS HISTORY CONFIGURATION
        // ==========================================

        builder.Entity<TicketStatusHistory>()
            .Property(h => h.OldStatus)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Entity<TicketStatusHistory>()
            .Property(h => h.NewStatus)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Entity<TicketStatusHistory>()
            .Property(h => h.Note)
            .HasMaxLength(1000);

        builder.Entity<TicketStatusHistory>()
            .HasOne(h => h.Ticket)
            .WithMany(t => t.StatusHistories)
            .HasForeignKey(h => h.TicketId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<TicketStatusHistory>()
            .HasOne(h => h.ChangedByUser)
            .WithMany(u => u.StatusChanges)
            .HasForeignKey(h => h.ChangedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}