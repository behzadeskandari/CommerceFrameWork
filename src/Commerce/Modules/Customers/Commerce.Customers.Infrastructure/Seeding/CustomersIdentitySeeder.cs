using System.Security.Claims;
using Commerce.Customers.Infrastructure.Security;
using Commerce.Framework.Contracts.Security;
using Commerce.Framework.Contracts.Seeding;
using Commerce.Framework.Data.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace Commerce.Customers.Infrastructure.Seeding;

public sealed class CustomersIdentitySeeder : IModuleSeeder
{
    public int Order => 10;

    public string Name => "Customers Identity";

    public string ModuleSystemName => "Commerce.Customers";

    public async Task SeedAsync(SeederContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        await using var scope = context.Services.CreateAsyncScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<CommerceIdentityRole>>();
        var permissionRegistry = scope.ServiceProvider.GetRequiredService<PermissionRegistry>();

        await EnsureRoleAsync(roleManager, CommerceRoles.Administrator, "Store administrator.", cancellationToken)
            .ConfigureAwait(false);
        await EnsureRoleAsync(roleManager, CommerceRoles.Customer, "Registered customer.", cancellationToken)
            .ConfigureAwait(false);

        var administratorRole = await roleManager.FindByNameAsync(CommerceRoles.Administrator).ConfigureAwait(false);
        if (administratorRole is null)
        {
            return;
        }

        var existingClaims = await roleManager.GetClaimsAsync(administratorRole).ConfigureAwait(false);
        var existingPermissions = existingClaims
            .Where(claim => claim.Type == CommerceClaimTypes.Permission)
            .Select(claim => claim.Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var permission in permissionRegistry.GetAllPermissions())
        {
            if (existingPermissions.Contains(permission.Name))
            {
                continue;
            }

            await roleManager.AddClaimAsync(
                administratorRole,
                new Claim(CommerceClaimTypes.Permission, permission.Name)).ConfigureAwait(false);
        }
    }

    private static async Task EnsureRoleAsync(
        RoleManager<CommerceIdentityRole> roleManager,
        string roleName,
        string description,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (await roleManager.RoleExistsAsync(roleName).ConfigureAwait(false))
        {
            return;
        }

        await roleManager.CreateAsync(new CommerceIdentityRole
        {
            Name = roleName,
            Description = description
        }).ConfigureAwait(false);
    }
}
