using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NXM.Tensai.Back.OKR.Domain;
using NXM.Tensai.Back.OKR.Domain.Enums;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace NXM.Tensai.Back.OKR.Infrastructure;

public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        var logger = serviceProvider.GetRequiredService<ILogger<SeedDataHostedService>>();
        var userManager = serviceProvider.GetRequiredService<UserManager<User>>();
        var roleManager = serviceProvider.GetRequiredService<RoleManager<Role>>();
        var dbContext = serviceProvider.GetRequiredService<OKRDbContext>();

        // First ensure roles exist
        logger.LogInformation("Seeding roles...");
        await RoleSeeder.SeedRolesAsync(roleManager);

        // Then create admin user and assign role
        logger.LogInformation("Ensuring admin user exists...");
        await EnsureAdminUserExistsAsync(userManager, roleManager, logger);

        // Verify seeding was successful
        logger.LogInformation("Verifying seed data...");
        await VerifySeedDataAsync(userManager, roleManager, logger);

        // --- Call MockOkrSeedData here ---
        await MockOkrSeedData.SeedMockOkrData(userManager, roleManager, dbContext, logger);
    }

    private static async Task EnsureAdminUserExistsAsync(UserManager<User> userManager, RoleManager<Role> roleManager, ILogger logger)
    {
        // Define your admin user details
        var adminEmail = "admin@parkihouni.com";
        var adminPassword = "AdminPassword123!";

        // Check if the admin user already exists
        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser == null)
        {
            // Create the admin user
            adminUser = new User
            {
                SupabaseId = "784951c9-55f3-4b7f-9d11-844815bed961",
                FirstName = "Admin",
                LastName = "User",
                Address = "System Address",
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                ProfilePictureUrl = "default-profile.jpg",
                Position = "System Administrator",
                IsEnabled = true,
                Gender = Gender.Male
            };

            var result = await userManager.CreateAsync(adminUser, adminPassword);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new ApplicationException($"Failed to create admin user: {errors}");
            }
            logger.LogInformation("Admin user created successfully");
        }

        // Ensure the user has the SuperAdmin role
        if (!await userManager.IsInRoleAsync(adminUser, "SuperAdmin"))
        {
            var result = await userManager.AddToRoleAsync(adminUser, "SuperAdmin");
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new ApplicationException($"Failed to assign SuperAdmin role: {errors}");
            }
            logger.LogInformation("SuperAdmin role assigned to admin user");
        }
    }

    private static async Task VerifySeedDataAsync(UserManager<User> userManager, RoleManager<Role> roleManager, ILogger logger)
    {
        // Verify admin user exists and has correct role
        var adminUser = await userManager.FindByEmailAsync("admin@parkihouni.com");
        if (adminUser == null)
        {
            throw new ApplicationException("Admin user was not created successfully");
        }

        var isInRole = await userManager.IsInRoleAsync(adminUser, "SuperAdmin");
        if (!isInRole)
        {
            throw new ApplicationException("Admin user is not in SuperAdmin role");
        }

        // Verify all roles exist
        foreach (var roleType in Enum.GetValues<RoleType>())
        {
            var role = await roleManager.FindByNameAsync(roleType.ToString());
            if (role == null)
            {
                throw new ApplicationException($"Role {roleType} was not created successfully");
            }
        }

        logger.LogInformation("Seed data verification completed successfully");
    }
}
