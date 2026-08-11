using System.Security.Claims;
using Commerce.Customers.Infrastructure.Security;
using Commerce.Framework.Contracts.Security;
using Commerce.Framework.Data.Identity;
using Microsoft.AspNetCore.Identity;

namespace Commerce.Customers.Infrastructure.Security;

public sealed class PermissionService(
    PermissionRegistry permissionRegistry,
    UserManager<CommerceIdentityUser> userManager,
    RoleManager<CommerceIdentityRole> roleManager) : IPermissionService
{
    public IReadOnlyList<PermissionDefinition> GetAllPermissions() =>
        permissionRegistry.GetAllPermissions();

    public async Task<IReadOnlyList<string>> GetPermissionsForUserAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var user = await userManager.FindByIdAsync(userId).ConfigureAwait(false);
        if (user is null)
        {
            return Array.Empty<string>();
        }

        var permissions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var claim in await userManager.GetClaimsAsync(user).ConfigureAwait(false))
        {
            if (claim.Type == CommerceClaimTypes.Permission)
            {
                permissions.Add(claim.Value);
            }
        }

        foreach (var roleName in await userManager.GetRolesAsync(user).ConfigureAwait(false))
        {
            var role = await roleManager.FindByNameAsync(roleName).ConfigureAwait(false);
            if (role is null)
            {
                continue;
            }

            foreach (var claim in await roleManager.GetClaimsAsync(role).ConfigureAwait(false))
            {
                if (claim.Type == CommerceClaimTypes.Permission)
                {
                    permissions.Add(claim.Value);
                }
            }
        }

        return permissions.OrderBy(permission => permission, StringComparer.Ordinal).ToList();
    }

    public async Task<bool> HasPermissionAsync(
        ClaimsPrincipal principal,
        string permission,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(principal);

        if (string.IsNullOrWhiteSpace(permission))
        {
            return false;
        }

        if (principal.HasClaim(CommerceClaimTypes.Permission, permission))
        {
            return true;
        }

        var userId = userManager.GetUserId(principal);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return false;
        }

        var permissions = await GetPermissionsForUserAsync(userId, cancellationToken).ConfigureAwait(false);
        return permissions.Contains(permission, StringComparer.OrdinalIgnoreCase);
    }
}
