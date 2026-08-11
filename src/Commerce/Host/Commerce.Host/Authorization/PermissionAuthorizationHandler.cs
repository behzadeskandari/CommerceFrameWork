using Commerce.Framework.Contracts.Security;
using Microsoft.AspNetCore.Authorization;

namespace Commerce.Host.Authorization;

public sealed class PermissionAuthorizationHandler(IPermissionService permissionService)
    : AuthorizationHandler<PermissionRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (await permissionService
                .HasPermissionAsync(context.User, requirement.Permission)
                .ConfigureAwait(false))
        {
            context.Succeed(requirement);
        }
    }
}
