using System;
using System.Threading.Tasks;
using ArtemisBanking.Core.Domain.Enums;
using ArtemisBanking.Infrastructure.Persistence.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace ArtemisBanking.Infrastructure.Identity.DependencyInjection;

public static class IdentitySeeder
{
    public static async Task SeedDefaultUsersAndRolesAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        string[] roles = { "Admin", "Cashier", "Client", "Merchant" };

        foreach (var roleName in roles)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole<Guid>(roleName));
            }
        }

        // Admin por defecto
        var adminUser = await userManager.FindByNameAsync("admin");
        if (adminUser is null)
        {
            adminUser = new ApplicationUser
            {
                UserName = "admin",
                Email = "admin@artemis.local",
                FirstName = "Admin",
                LastName = "Principal",
                NationalId = "00000000000",
                Role = UserRole.Admin,
                IsActive = true,
                EmailConfirmed = true
            };

            // OJO: password de pruebas
            var result = await userManager.CreateAsync(adminUser, "Admin123!");

            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }
            else
            {
                // En un caso real podrías loguear errores aquí
            }
        }
    }
}
