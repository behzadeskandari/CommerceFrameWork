using Commerce.Host.Integration;
using Commerce.Integration.Contracts.ApiClients;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Commerce.Host.Integration;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class RequireApiScopeAttribute(string scope) : Attribute, IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var auth = context.HttpContext.GetApiClientAuthentication();
        if (auth is null || !auth.IsAuthenticated)
        {
            context.Result = new UnauthorizedObjectResult(new { success = false, error = "Authentication required." });
            return;
        }

        if (!auth.Scopes.Contains(scope, StringComparer.OrdinalIgnoreCase))
        {
            context.Result = new ObjectResult(new { success = false, error = "Insufficient API scope." })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
            return;
        }

        await next().ConfigureAwait(false);
    }
}
