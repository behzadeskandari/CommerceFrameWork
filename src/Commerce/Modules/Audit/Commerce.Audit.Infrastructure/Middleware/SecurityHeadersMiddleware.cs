using Microsoft.AspNetCore.Http;

namespace Commerce.Audit.Infrastructure.Middleware;

public sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.Headers.TryAdd("X-Content-Type-Options", "nosniff");
        context.Response.Headers.TryAdd("X-Frame-Options", "DENY");
        context.Response.Headers.TryAdd("Referrer-Policy", "strict-origin-when-cross-origin");
        context.Response.Headers.TryAdd("X-XSS-Protection", "0");
        context.Response.Headers.TryAdd("Permissions-Policy", "camera=(), microphone=(), geolocation=()");
        context.Response.Headers.TryAdd(
            "Content-Security-Policy",
            "default-src 'self'; frame-ancestors 'none'; base-uri 'self'; form-action 'self'");

        await next(context).ConfigureAwait(false);
    }
}
