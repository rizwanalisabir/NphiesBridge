using Microsoft.AspNetCore.Identity;
using NphiesBridge.Core.Entities;

namespace NphiesBridge.API.Data
{
    public static class SeedData
    {
        public static async Task SeedDefaultUsers(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

            // Ensure roles exist
            await EnsureRoleExists(roleManager, "Admin", "System Administrator");
            await EnsureRoleExists(roleManager, "Provider", "Healthcare Provider User");

            // Create users
            await CreateAdminUser(userManager, "admin@nphiesbridge.com", "Admin123!", "System", "Administrator");
            await CreateAdminUser(userManager, "admin2@nphiesbridge.com", "Admin123!", "John", "Smith");
            await CreateProviderUser(userManager, "provider@hospital.com", "Provider123!", "Jane", "Doe");
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

        private static async Task CreateAdminUser(UserManager<ApplicationUser> userManager, string email, string password, string firstName, string lastName)
        {
            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    FirstName = firstName,
                    LastName = lastName,
                    EmailConfirmed = true,
                    IsActive = true
                };

                var result = await userManager.CreateAsync(user, password);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, "Admin");
                    Console.WriteLine($"Admin user created: {email} / {password}");
                }
                else
                {
                    Console.WriteLine($"Failed to create admin user {email}:");
                    foreach (var error in result.Errors)
                    {
                        Console.WriteLine($"- {error.Description}");
                    }
                }
            }
        }

        private static async Task CreateProviderUser(UserManager<ApplicationUser> userManager, string email, string password, string firstName, string lastName)
        {
            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    FirstName = firstName,
                    LastName = lastName,
                    EmailConfirmed = true,
                    IsActive = true
                    // HealthProviderId can be set later when you have providers
                };

                var result = await userManager.CreateAsync(user, password);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, "Provider");
                    Console.WriteLine($"Provider user created: {email} / {password}");
                }
                else
                {
                    Console.WriteLine($"Failed to create provider user {email}:");
                    foreach (var error in result.Errors)
                    {
                        Console.WriteLine($"- {error.Description}");
                    }
                }
            }
        }
    }
}