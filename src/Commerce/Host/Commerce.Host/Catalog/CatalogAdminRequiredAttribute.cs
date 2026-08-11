using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;

namespace Commerce.Host.Catalog;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class CatalogAdminRequiredAttribute : Attribute, IAuthorizationFilter
{
    public const string AdminKeyHeader = "X-Commerce-Catalog-Admin-Key";

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var configuration = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        var configuredKey = configuration["Commerce:Catalog:AdminApiKey"];

        if (string.IsNullOrWhiteSpace(configuredKey))
        {
            context.Result = new ObjectResult(new
            {
                message = "Catalog mutation endpoints are disabled until Commerce:Catalog:AdminApiKey is configured."
            })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
            return;
        }

        if (!context.HttpContext.Request.Headers.TryGetValue(AdminKeyHeader, out var providedKey) ||
            !string.Equals(providedKey.ToString(), configuredKey, StringComparison.Ordinal))
        {
            context.Result = new UnauthorizedObjectResult(new
            {
                message = "Valid catalog admin API key is required."
            });
        }
    }
}
