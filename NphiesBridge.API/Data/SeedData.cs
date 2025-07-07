using Microsoft.AspNetCore.Identity;
using NphiesBridge.Core.Entities;

namespace NphiesBridge.API.Data
{
    public static class SeedData
    {
        public static async Task SeedDefaultAdmin(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

            // Ensure roles exist
            await EnsureRoleExists(roleManager, "Admin", "System Administrator");
            await EnsureRoleExists(roleManager, "Provider", "Healthcare Provider User");

            // Create default admin user
            await CreateDefaultAdmin(userManager);
        }

        private static async Task EnsureRoleExists(RoleManager<ApplicationRole> roleManager, string roleName, string description)
        {
            var role = await roleManager.FindByNameAsync(roleName);
            if (role == null)
            {
                role = new ApplicationRole(roleName)
                {
                    Description = description
                };
                await roleManager.CreateAsync(role);
            }
        }

        private static async Task CreateDefaultAdmin(UserManager<ApplicationUser> userManager)
        {
            const string adminEmail = "admin@nphiesbridge.com";
            const string adminPassword = "Admin123!";

            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FirstName = "System",
                    LastName = "Administrator",
                    EmailConfirmed = true,
                    IsActive = true
                };

                var result = await userManager.CreateAsync(adminUser, adminPassword);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                    Console.WriteLine($"Default admin created: {adminEmail} / {adminPassword}");
                }
                else
                {
                    Console.WriteLine("Failed to create default admin:");
                    foreach (var error in result.Errors)
                    {
                        Console.WriteLine($"- {error.Description}");
                    }
                }
            }
        }
    }
}