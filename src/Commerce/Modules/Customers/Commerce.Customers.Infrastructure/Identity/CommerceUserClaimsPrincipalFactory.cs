using System.Security.Claims;
using Commerce.Customers.Application.Abstractions;
using Commerce.Framework.Contracts.Security;
using Commerce.Framework.Data.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Commerce.Customers.Infrastructure.Identity;

public sealed class CommerceUserClaimsPrincipalFactory(
    UserManager<CommerceIdentityUser> userManager,
    RoleManager<CommerceIdentityRole> roleManager,
    IOptions<IdentityOptions> optionsAccessor,
    ICustomerRepository customerRepository) : UserClaimsPrincipalFactory<CommerceIdentityUser, CommerceIdentityRole>(
    userManager,
    roleManager,
    optionsAccessor)
{
    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(CommerceIdentityUser user)
    {
        var identity = await base.GenerateClaimsAsync(user).ConfigureAwait(false);

        var customer = await customerRepository.GetByIdentityUserIdAsync(user.Id).ConfigureAwait(false);
        if (customer is not null)
        {
            identity.AddClaim(new Claim(CommerceClaimTypes.CustomerId, customer.Id.ToString()));
        }

        var permissions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var claim in await UserManager.GetClaimsAsync(user).ConfigureAwait(false))
        {
            if (claim.Type == CommerceClaimTypes.Permission)
            {
                permissions.Add(claim.Value);
            }
        }

        foreach (var roleName in await UserManager.GetRolesAsync(user).ConfigureAwait(false))
        {
            var role = await RoleManager.FindByNameAsync(roleName).ConfigureAwait(false);
            if (role is null)
            {
                continue;
            }

            foreach (var claim in await RoleManager.GetClaimsAsync(role).ConfigureAwait(false))
            {
                if (claim.Type == CommerceClaimTypes.Permission)
                {
                    permissions.Add(claim.Value);
                }
            }
        }

        foreach (var permission in permissions)
        {
            identity.AddClaim(new Claim(CommerceClaimTypes.Permission, permission));
        }

        return identity;
    }
}
