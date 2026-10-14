using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Whistleblowing.Domain.Entities;
using Whistleblowing.Domain.Enums;

namespace Whistleblowing.Infrastructure.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;

        var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("DbSeeder");
        var roleManager = sp.GetRequiredService<RoleManager<ApplicationRole>>();
        var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();
        var context = sp.GetRequiredService<AppDbContext>();
        var config = sp.GetRequiredService<IConfiguration>();

        // 1. Seed Roles
        foreach (var roleName in RoleNames.All)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new ApplicationRole(roleName, $"{roleName} role"));
                logger.LogInformation("Seeded role: {RoleName}", roleName);
            }
        }

        // 2. Seed Admin user
        var adminEmail = config["Seed:AdminEmail"] ?? "admin@wb.local";
        var adminPassword = config["Seed:AdminPassword"] ?? "Admin@12345";

        var admin = await userManager.FindByEmailAsync(adminEmail);
        if (admin == null)
        {
            admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                FullName = "System Administrator",
                Status = UserStatus.Active,
            };

            var result = await userManager.CreateAsync(admin, adminPassword);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(admin, RoleNames.Admin);
                logger.LogInformation("Seeded admin: {Email}", adminEmail);
            }
            else
            {
                logger.LogError("Failed to seed admin: {Errors}",
                    string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }

        // 3. Seed sample departments
        if (!context.Departments.Any())
        {
            context.Departments.AddRange(
                new Department { Name = "Human Resources", Description = "HR department" },
                new Department { Name = "Information Technology", Description = "IT department" },
                new Department { Name = "Finance", Description = "Finance department" },
                new Department { Name = "Operations", Description = "Operations department" }
            );
            await context.SaveChangesAsync();
            logger.LogInformation("Seeded 4 sample departments");
        }
    }
}