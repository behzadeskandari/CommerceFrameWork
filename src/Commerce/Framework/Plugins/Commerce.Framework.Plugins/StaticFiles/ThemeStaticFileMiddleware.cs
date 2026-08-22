using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;

namespace Commerce.Framework.Plugins.StaticFiles;

public static class ThemeStaticFileMiddlewareExtensions
{
    public static IApplicationBuilder UseThemeStaticFiles(this IApplicationBuilder app) =>
        app.UseMiddleware<ThemeStaticFileMiddleware>();
}

public sealed class ThemeStaticFileMiddleware(
    RequestDelegate next,
    IHostEnvironment hostEnvironment)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        if (!path.StartsWith("/themes/", StringComparison.OrdinalIgnoreCase))
        {
            await next(context).ConfigureAwait(false);
            return;
        }

        var segments = path.Split(
            '/',
            StringSplitOptions.RemoveEmptyEntries);

        if (segments.Length < 3)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        var themeName = segments[1];

        if (themeName.Contains("..", StringComparison.Ordinal) ||
            themeName.Contains('\\') ||
            themeName.Contains('/'))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        var relativePath = string.Join('/', segments.Skip(2));

        if (string.IsNullOrWhiteSpace(relativePath) ||
            relativePath.Contains("..", StringComparison.Ordinal))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        var themesRoot = Path.Combine(
            hostEnvironment.ContentRootPath,
            "themes");

        var themeDirectory = Path.Combine(
            themesRoot,
            themeName);

        var filePath = Path.GetFullPath(
            Path.Combine(
                themeDirectory,
                relativePath.Replace(
                    '/',
                    Path.DirectorySeparatorChar)));

        var normalizedThemeDirectory =
            Path.GetFullPath(themeDirectory);

        if (!filePath.StartsWith(
                normalizedThemeDirectory,
                StringComparison.OrdinalIgnoreCase))
        {
            context.Response.StatusCode =
                StatusCodes.Status400BadRequest;

            return;
        }

        if (!File.Exists(filePath))
        {
            context.Response.StatusCode =
                StatusCodes.Status404NotFound;

            return;
        }

        context.Response.ContentType =
            GetContentType(filePath);

        await context.Response
            .SendFileAsync(filePath)
            .ConfigureAwait(false);
    }

    private static string GetContentType(string filePath)
    {
        var extension =
            Path.GetExtension(filePath)
                .ToLowerInvariant();

        return extension switch
        {
            ".css" => "text/css; charset=utf-8",
            ".js" => "application/javascript; charset=utf-8",
            ".json" => "application/json; charset=utf-8",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".svg" => "image/svg+xml",
            ".woff" => "font/woff",
            ".woff2" => "font/woff2",
            ".ttf" => "font/ttf",
            ".html" => "text/html; charset=utf-8",
            _ => "application/octet-stream"
        };
    }
}