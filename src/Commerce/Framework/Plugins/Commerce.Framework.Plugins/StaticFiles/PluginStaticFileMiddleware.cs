using Commerce.Framework.Plugins.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Commerce.Framework.Plugins.StaticFiles;

public static class PluginStaticFileMiddlewareExtensions
{
    public static IApplicationBuilder UsePluginStaticFiles(this IApplicationBuilder app) =>
        app.UseMiddleware<PluginStaticFileMiddleware>();
}

public sealed class PluginStaticFileMiddleware(
    RequestDelegate next,
    IHostEnvironment hostEnvironment,
    IOptions<CommercePluginOptions> options)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        if (!path.StartsWith("/plugins/", StringComparison.OrdinalIgnoreCase))
        {
            await next(context).ConfigureAwait(false);
            return;
        }

        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length < 3)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        var systemName = segments[1];
        if (systemName.Contains("..", StringComparison.Ordinal) ||
            systemName.Contains('\\') ||
            systemName.Contains('/'))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        var relativePath = string.Join('/', segments.Skip(2));
        if (string.IsNullOrWhiteSpace(relativePath) || relativePath.Contains("..", StringComparison.Ordinal))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        var rootPath = ResolveRootPath();
        var pluginDirectory = Path.Combine(rootPath, systemName);
        var filePath = Path.GetFullPath(Path.Combine(pluginDirectory, relativePath.Replace('/', Path.DirectorySeparatorChar)));
        var normalizedRoot = Path.GetFullPath(pluginDirectory);

        if (!filePath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase) || !File.Exists(filePath))
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        var contentType = GetContentType(filePath);
        context.Response.ContentType = contentType;
        await context.Response.SendFileAsync(filePath).ConfigureAwait(false);
    }

    private string ResolveRootPath()
    {
        var configuredRoot = options.Value.RootPath;
        return Path.IsPathRooted(configuredRoot)
            ? configuredRoot
            : Path.Combine(hostEnvironment.ContentRootPath, configuredRoot);
    }

    private static string GetContentType(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return extension switch
        {
            ".css" => "text/css",
            ".js" => "application/javascript",
            ".json" => "application/json",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".svg" => "image/svg+xml",
            ".html" => "text/html",
            _ => "application/octet-stream"
        };
    }
}
