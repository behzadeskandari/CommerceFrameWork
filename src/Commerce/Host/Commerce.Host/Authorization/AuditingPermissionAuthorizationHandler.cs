using Commerce.Framework.Contracts.Audit;
using Commerce.Framework.Contracts.Security;
using Commerce.Host.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Commerce.Host.Authorization;

public sealed class AuditingPermissionAuthorizationHandler(
    IPermissionService permissionService,
    IAuditPublisher auditPublisher) : AuthorizationHandler<PermissionRequirement>
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
            return;
        }

        var httpContext = ResolveHttpContext(context);
        if (httpContext is not null)
        {
            await auditPublisher.PublishAsync(new AuditPublishRequest(
                AuditCategory.Authorization,
                AuditActions.AccessDenied,
                Success: false,
                EntityType: "Permission",
                EntityId: requirement.Permission,
                Details: new Dictionary<string, string?>
                {
                    ["path"] = httpContext.Request.Path.Value,
                    ["method"] = httpContext.Request.Method
                }), httpContext.RequestAborted).ConfigureAwait(false);
        }

        context.Fail();
    }

    private static HttpContext? ResolveHttpContext(AuthorizationHandlerContext context) =>
        context.Resource switch
        {
            HttpContext httpContext => httpContext,
            AuthorizationFilterContext filterContext => filterContext.HttpContext,
            _ => null
        };
}
