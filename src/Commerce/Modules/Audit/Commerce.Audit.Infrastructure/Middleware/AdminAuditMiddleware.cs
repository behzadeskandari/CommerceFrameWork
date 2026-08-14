using Commerce.Framework.Contracts.Audit;
using Microsoft.AspNetCore.Http;

namespace Commerce.Audit.Infrastructure.Middleware;

public sealed class AdminAuditMiddleware(RequestDelegate next)
{
    private static readonly HashSet<string> MutatingMethods =
        new(StringComparer.OrdinalIgnoreCase) { "POST", "PUT", "PATCH", "DELETE" };

    public async Task InvokeAsync(HttpContext context, IAuditPublisher auditPublisher)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        var isAdminPath = path.StartsWith("/api/admin", StringComparison.OrdinalIgnoreCase);
        var isMutating = MutatingMethods.Contains(context.Request.Method);

        await next(context).ConfigureAwait(false);

        if (!isAdminPath || !isMutating)
        {
            return;
        }

        var success = context.Response.StatusCode < 400;
        await auditPublisher.PublishAsync(new AuditPublishRequest(
            AuditCategory.Admin,
            AuditActions.AdminRequest,
            Success: success,
            EntityType: "HttpRequest",
            EntityId: path,
            Details: new Dictionary<string, string?>
            {
                ["method"] = context.Request.Method,
                ["statusCode"] = context.Response.StatusCode.ToString()
            }), context.RequestAborted).ConfigureAwait(false);
    }
}
