using Commerce.Integration.Contracts.ApiClients;

namespace Commerce.Host.Integration;

public sealed class ApiKeyAuthenticationMiddleware(
    RequestDelegate next,
    IApiClientAuthenticator authenticator)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Path.StartsWithSegments("/api/external", StringComparison.OrdinalIgnoreCase))
        {
            await next(context).ConfigureAwait(false);
            return;
        }

        var apiKey = context.Request.Headers["X-Api-Key"].FirstOrDefault()
            ?? context.Request.Headers.Authorization.FirstOrDefault()?.Replace("Bearer ", "", StringComparison.OrdinalIgnoreCase);

        var auth = await authenticator.AuthenticateAsync(apiKey, context.RequestAborted).ConfigureAwait(false);
        if (!auth.IsAuthenticated)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new
            {
                success = false,
                error = auth.FailureReason ?? "Authentication required."
            }).ConfigureAwait(false);
            return;
        }

        context.Items[ApiClientContextKeys.Authentication] = auth;
        await next(context).ConfigureAwait(false);
    }
}

public static class ApiKeyAuthenticationMiddlewareExtensions
{
    public static IApplicationBuilder UseApiKeyAuthentication(this IApplicationBuilder app) =>
        app.UseMiddleware<ApiKeyAuthenticationMiddleware>();
}
