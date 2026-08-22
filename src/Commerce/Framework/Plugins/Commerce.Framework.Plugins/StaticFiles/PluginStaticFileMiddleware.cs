using Commerce.Framework.Plugins.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
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

        if (path.StartsWith("/themes/", StringComparison.OrdinalIgnoreCase))
        {
            await ServeThemeAssetAsync(context).ConfigureAwait(false);
            return;
        }

        if (path.StartsWith("/plugins/", StringComparison.OrdinalIgnoreCase))
        {
            await ServePluginAssetAsync(context).ConfigureAwait(false);
            return;
        }

        await next(context).ConfigureAwait(false);
    }

    private async Task ServeThemeAssetAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        var segments = path.Split(
            '/',
            StringSplitOptions.RemoveEmptyEntries);

        // /themes/{themeSystemName}/{relativePath}
        if (segments.Length < 3)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        var themeSystemName = segments[1];

        if (!IsSafeSegment(themeSystemName))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        var relativePath = string.Join('/', segments.Skip(2));

        if (!IsSafeRelativePath(relativePath))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        var rootPath = ResolveRootPath();

        /*
         * Expected:
         *
         * {RootPath}/Commerce.Plugin.Theme.Default/
         *     themes/default/theme.css
         *
         * OR:
         *
         * {RootPath}/Commerce.Plugin.Theme.Default/
         *     Assets/themes/default/theme.css
         *
         * depending on your plugin packaging convention.
         */

        var pluginSystemName = ResolveThemePluginSystemName(themeSystemName);

        if (pluginSystemName is null)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        var pluginDirectory = Path.Combine(rootPath, pluginSystemName);

        var candidatePaths = new[]
        {
            Path.Combine(
                pluginDirectory,
                "themes",
                themeSystemName,
                relativePath.Replace('/', Path.DirectorySeparatorChar)),

            Path.Combine(
                pluginDirectory,
                "Assets",
                "themes",
                themeSystemName,
                relativePath.Replace('/', Path.DirectorySeparatorChar)),

            Path.Combine(
                pluginDirectory,
                "wwwroot",
                "themes",
                themeSystemName,
                relativePath.Replace('/', Path.DirectorySeparatorChar))
        };

        var filePath = candidatePaths
            .Select(Path.GetFullPath)
            .FirstOrDefault(path =>
                IsPathInsideDirectory(path, Path.GetFullPath(pluginDirectory)) &&
                File.Exists(path));

        if (filePath is null)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        await SendFileAsync(context, filePath).ConfigureAwait(false);
    }

    private async Task ServePluginAssetAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        var segments = path.Split(
            '/',
            StringSplitOptions.RemoveEmptyEntries);

        // /plugins/{pluginSystemName}/{relativePath}
        if (segments.Length < 3)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        var systemName = segments[1];

        if (!IsSafeSegment(systemName))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        var relativePath = string.Join('/', segments.Skip(2));

        if (!IsSafeRelativePath(relativePath))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        var rootPath = ResolveRootPath();
        var pluginDirectory = Path.Combine(rootPath, systemName);

        var filePath = Path.GetFullPath(
            Path.Combine(
                pluginDirectory,
                relativePath.Replace('/', Path.DirectorySeparatorChar)));

        var normalizedRoot = Path.GetFullPath(pluginDirectory);

        if (!IsPathInsideDirectory(filePath, normalizedRoot) ||
            !File.Exists(filePath))
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        await SendFileAsync(context, filePath).ConfigureAwait(false);
    }

    private string? ResolveThemePluginSystemName(string themeSystemName)
    {
        /*
         * Current convention:
         *
         * default theme
         *     ->
         * Commerce.Plugin.Theme.Default
         *
         * If your plugin manifest already exposes the mapping,
         * replace this method with the real plugin registry lookup.
         */

        if (themeSystemName.Equals(
                "default",
                StringComparison.OrdinalIgnoreCase))
        {
            return "Commerce.Plugin.Theme.Default";
        }

        return null;
    }

    private string ResolveRootPath()
    {
        var configuredRoot = options.Value.RootPath;

        return Path.IsPathRooted(configuredRoot)
            ? configuredRoot
            : Path.Combine(
                hostEnvironment.ContentRootPath,
                configuredRoot);
    }

    private static bool IsSafeSegment(string value)
    {
        return !string.IsNullOrWhiteSpace(value)
            && !value.Contains("..", StringComparison.Ordinal)
            && !value.Contains('\\')
            && !value.Contains('/');
    }

    private static bool IsSafeRelativePath(string value)
    {
        return !string.IsNullOrWhiteSpace(value)
            && !value.Contains("..", StringComparison.Ordinal)
            && !value.StartsWith('/')
            && !value.StartsWith('\\');
    }

    private static bool IsPathInsideDirectory(
        string filePath,
        string directory)
    {
        var normalizedFile = Path.GetFullPath(filePath)
            .TrimEnd(
                Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar);

        var normalizedDirectory = Path.GetFullPath(directory)
            .TrimEnd(
                Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar);

        return normalizedFile.StartsWith(
            normalizedDirectory + Path.DirectorySeparatorChar,
            StringComparison.OrdinalIgnoreCase);
    }

    private static async Task SendFileAsync(
        HttpContext context,
        string filePath)
    {
        context.Response.ContentType = GetContentType(filePath);

        await context.Response.SendFileAsync(filePath)
            .ConfigureAwait(false);
    }

    private static string GetContentType(string filePath)
    {
        var extension = Path.GetExtension(filePath)
            .ToLowerInvariant();

        return extension switch
        {
            ".css" => "text/css",
            ".js" => "application/javascript",
            ".json" => "application/json",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".svg" => "image/svg+xml",
            ".ico" => "image/x-icon",
            ".woff" => "font/woff",
            ".woff2" => "font/woff2",
            ".ttf" => "font/ttf",
            ".html" => "text/html",
            _ => "application/octet-stream"
        };
    }
}
//public static class PluginStaticFileMiddlewareExtensions
//{
//    public static IApplicationBuilder UsePluginStaticFiles(this IApplicationBuilder app) =>
//        app.UseMiddleware<PluginStaticFileMiddleware>();
//}

//public sealed class PluginStaticFileMiddleware(
//    RequestDelegate next,
//    IHostEnvironment hostEnvironment,
//    IOptions<CommercePluginOptions> options)
//{
//    public async Task InvokeAsync(HttpContext context)
//    {
//        var path = context.Request.Path.Value ?? string.Empty;
//        if (!path.StartsWith("/plugins/", StringComparison.OrdinalIgnoreCase))
//        {
//            await next(context).ConfigureAwait(false);
//            return;
//        }

//        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
//        if (segments.Length < 3)
//        {
//            context.Response.StatusCode = StatusCodes.Status404NotFound;
//            return;
//        }

//        var systemName = segments[1];
//        if (systemName.Contains("..", StringComparison.Ordinal) ||
//            systemName.Contains('\\') ||
//            systemName.Contains('/'))
//        {
//            context.Response.StatusCode = StatusCodes.Status400BadRequest;
//            return;
//        }

//        var relativePath = string.Join('/', segments.Skip(2));
//        if (string.IsNullOrWhiteSpace(relativePath) || relativePath.Contains("..", StringComparison.Ordinal))
//        {
//            context.Response.StatusCode = StatusCodes.Status400BadRequest;
//            return;
//        }

//        var rootPath = ResolveRootPath();
//        var pluginDirectory = Path.Combine(rootPath, systemName);
//        var filePath = Path.GetFullPath(Path.Combine(pluginDirectory, relativePath.Replace('/', Path.DirectorySeparatorChar)));
//        var normalizedRoot = Path.GetFullPath(pluginDirectory);

//        if (!filePath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase) || !File.Exists(filePath))
//        {
//            context.Response.StatusCode = StatusCodes.Status404NotFound;
//            return;
//        }

//        var contentType = GetContentType(filePath);
//        context.Response.ContentType = contentType;
//        await context.Response.SendFileAsync(filePath).ConfigureAwait(false);
//    }

//    private string ResolveRootPath()
//    {
//        var configuredRoot = options.Value.RootPath;
//        return Path.IsPathRooted(configuredRoot)
//            ? configuredRoot
//            : Path.Combine(hostEnvironment.ContentRootPath, configuredRoot);
//    }

//    private static string GetContentType(string filePath)
//    {
//        var extension = Path.GetExtension(filePath).ToLowerInvariant();
//        return extension switch
//        {
//            ".css" => "text/css",
//            ".js" => "application/javascript",
//            ".json" => "application/json",
//            ".png" => "image/png",
//            ".jpg" or ".jpeg" => "image/jpeg",
//            ".svg" => "image/svg+xml",
//            ".html" => "text/html",
//            _ => "application/octet-stream"
//        };
//    }
//}
