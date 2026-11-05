using ITHelpdesk.Web.Models.Entities;
using ITHelpdesk.Web.Models.Enums;
using Microsoft.AspNetCore.Identity;

namespace ITHelpdesk.Web.Data;

public static class DevelopmentDataSeeder
{
    public const string DevelopmentUserEmail =
        "dev.employee@ithelpdesk.local";

    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        var roleManager = serviceProvider
            .GetRequiredService<RoleManager<IdentityRole>>();

        var userManager = serviceProvider
            .GetRequiredService<UserManager<ApplicationUser>>();

        var configuration = serviceProvider
            .GetRequiredService<IConfiguration>();

        // ==========================================
        // CREATE ROLES
        // ==========================================

        string[] roles =
        [
            "Admin",
            "IT Support",
            "Employee"
        ];

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                var result = await roleManager.CreateAsync(
                    new IdentityRole(role)
                );

                EnsureSuccess(
                    result,
                    $"Failed to create role '{role}'."
                );
            }
        }

        // ==========================================
        // CREATE DEVELOPMENT USERS
        // ==========================================

        await SeedUserAsync(userManager, configuration, "Admin");

        await SeedUserAsync(userManager, configuration, "ITSupport");

        await SeedUserAsync(userManager, configuration, "Employee");
    }

    private static async Task SeedUserAsync(
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration,
        string userType)
    {
        // ==========================================
        // READ USER SECRETS
        // ==========================================

        var email = configuration[
            $"DevelopmentUsers:{userType}:Email"
        ];

        var password = configuration[
            $"DevelopmentUsers:{userType}:Password"
        ];

        var fullName = configuration[
            $"DevelopmentUsers:{userType}:FullName"
        ];

        if (string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(password) ||
            string.IsNullOrWhiteSpace(fullName))
        {
            throw new InvalidOperationException(
                $"Missing User Secrets configuration for '{userType}'."
            );
        }

        // ==========================================
        // MAP USER TYPE TO ROLE
        // ==========================================

        var role = userType switch
        {
            "Admin" => "Admin",
            "ITSupport" => "IT Support",
            "Employee" => "Employee",
            _ => throw new InvalidOperationException(
                $"Unknown user type '{userType}'."
            )
        };

        // ==========================================
        // FIND OR CREATE USER
        // ==========================================

        var user = await userManager.FindByEmailAsync(email);

        if (user == null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FullName = fullName,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow,

                // Development accounts are pre-approved.
                ApprovalStatus = AccountApprovalStatus.Approved
            };

            var createResult = await userManager.CreateAsync(
                user,
                password
            );

            EnsureSuccess(
                createResult,
                $"Failed to create user '{email}'."
            );
        }
        else
        {
            // Update existing development account.
            user.UserName = email;
            user.Email = email;
            user.EmailConfirmed = true;
            user.FullName = fullName;
            user.IsActive = true;

            // Keep development accounts approved.
            user.ApprovalStatus = AccountApprovalStatus.Approved;

            var updateResult = await userManager.UpdateAsync(user);

            EnsureSuccess(
                updateResult,
                $"Failed to update user '{email}'."
            );

            // Add password to older accounts if necessary.
            if (!await userManager.HasPasswordAsync(user))
            {
                var passwordResult = await userManager.AddPasswordAsync(
                    user,
                    password
                );

                EnsureSuccess(
                    passwordResult,
                    $"Failed to set password for '{email}'."
                );
            }
        }

        // ==========================================
        // ASSIGN USER ROLE
        // ==========================================

        var currentRoles = await userManager.GetRolesAsync(user);

        var otherRoles = currentRoles
            .Where(existingRole => existingRole != role)
            .ToList();

        if (otherRoles.Count > 0)
        {
            var removeResult = await userManager.RemoveFromRolesAsync(
                user,
                otherRoles
            );

            EnsureSuccess(
                removeResult,
                $"Failed to remove other roles from '{email}'."
            );
        }

        if (!await userManager.IsInRoleAsync(user, role))
        {
            var addRoleResult = await userManager.AddToRoleAsync(
                user,
                role
            );

            EnsureSuccess(
                addRoleResult,
                $"Failed to assign role '{role}' to '{email}'."
            );
        }
    }

    // ==========================================
    // VALIDATE IDENTITY RESULT
    // ==========================================

    private static void EnsureSuccess(
        IdentityResult result,
        string errorMessage)
    {
        if (!result.Succeeded)
        {
            var errors = string.Join(
                "; ",
                result.Errors.Select(error => error.Description)
            );

            throw new InvalidOperationException(
                $"{errorMessage} Details: {errors}"
            );
        }
    }
}