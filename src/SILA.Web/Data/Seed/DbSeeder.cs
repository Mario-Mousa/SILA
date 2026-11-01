// ═══════════ HANDOFF ═══════════════════════════════════
// Task:        T1.2 | Owner: P1 | Status: DONE
// What exists: DbSeeder creating the 4 Identity roles and one default Admin account
// What is left: // TODO(T2.4): Extend DbSeeder with demo accounts (>=2 orgs per role, >=50 listings, requests, orders)
// Depends on:  T1.1 (ApplicationDbContext, ApplicationUser)
// Used by:     Program.cs (called at application startup in Development)
// How to test: Run the app; check AspNetRoles and AspNetUsers tables for Brand, Factory, Supplier, Admin roles and Admin user
// Notes:       Admin credentials: admin@sila.com / Admin@123456 (demo only; documented in README)
// ═══════════════════════════════════════════════════════

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SILA.Web.Models.Entities;

namespace SILA.Web.Data.Seed;

public static class DbSeeder
{
    public const string RoleAdmin = "Admin";
    public const string RoleBrand = "Brand";
    public const string RoleFactory = "Factory";
    public const string RoleSupplier = "Supplier";

    public static readonly string[] Roles = [RoleAdmin, RoleBrand, RoleFactory, RoleSupplier];

    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DbSeeder");

        try
        {
            // 1. Seed Roles
            foreach (var roleName in Roles)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    var result = await roleManager.CreateAsync(new IdentityRole(roleName));
                    if (result.Succeeded)
                    {
                        logger.LogInformation("Seeded Identity role: {Role}", roleName);
                    }
                    else
                    {
                        logger.LogError("Failed to create role {Role}: {Errors}",
                            roleName, string.Join(", ", result.Errors.Select(e => e.Description)));
                    }
                }
            }

            // 2. Seed Default Admin User (D-02: Lightweight Admin created by seeder, no admin sign-up)
            const string adminEmail = "admin@sila.com";
            var existingAdmin = await userManager.FindByEmailAsync(adminEmail);
            if (existingAdmin == null)
            {
                var adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FullName = "SILA System Administrator",
                    EmailConfirmed = true,
                    PhoneNumber = "01000000000"
                };

                // Default development password for demo
                var createResult = await userManager.CreateAsync(adminUser, "Admin@123456");
                if (createResult.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, RoleAdmin);
                    logger.LogInformation("Seeded default Admin user: {Email}", adminEmail);
                }
                else
                {
                    logger.LogError("Failed to create default Admin user: {Errors}",
                        string.Join(", ", createResult.Errors.Select(e => e.Description)));
                }
            }

            // TODO(T2.4): P4 will extend this method to seed:
            // - Demo organizations (Brand, Factory, Supplier) with verified profiles
            // - At least 50 MaterialListing entries across categories
            // - Sample ManufacturingRequest rounds and MaterialOrder records for volume testing
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding the database.");
        }
    }
}

