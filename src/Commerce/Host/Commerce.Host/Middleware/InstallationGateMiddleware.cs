using Commerce.Framework.Contracts.Installation;

namespace Commerce.Host.Middleware;

public sealed class InstallationGateMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, IInstallationStateService stateService)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        var isInstallationPath = path.StartsWith("/installation", StringComparison.OrdinalIgnoreCase);

        if (await stateService.IsInstallationLockedAsync(context.RequestAborted).ConfigureAwait(false))
        {
            if (isInstallationPath)
            {
                context.Response.StatusCode = StatusCodes.Status409Conflict;
                await context.Response.WriteAsJsonAsync(new
                {
                    message = "Commerce is already installed. The installation wizard is locked."
                }).ConfigureAwait(false);
                return;
            }
        }
        else if (!isInstallationPath &&
                 !path.StartsWith("/health", StringComparison.OrdinalIgnoreCase) &&
                 path != "/")
        {
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            await context.Response.WriteAsJsonAsync(new
            {
                message = "Commerce is not installed. Visit /installation to begin setup."
            }).ConfigureAwait(false);
            return;
        }

        await next(context).ConfigureAwait(false);
    }
}
