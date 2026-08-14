using System.Diagnostics;
using Commerce.Framework.Application.Observability;
using Commerce.Framework.Contracts.Observability;
using Commerce.Observability.Application.Logging;
using Commerce.Observability.Infrastructure.Correlation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Commerce.Observability.Infrastructure.Middleware;

public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = ResolveHeader(context, ObservabilityHeaders.CorrelationId)
            ?? Guid.NewGuid().ToString("N");
        var requestId = ResolveHeader(context, ObservabilityHeaders.RequestId)
            ?? Guid.NewGuid().ToString("N");

        context.Items[CorrelationContextKeys.CorrelationId] = correlationId;
        context.Items[CorrelationContextKeys.RequestId] = requestId;
        context.Response.Headers[ObservabilityHeaders.CorrelationId] = correlationId;
        context.Response.Headers[ObservabilityHeaders.RequestId] = requestId;

        using var activity = CommerceTracing.StartCommerceActivity("http.request", correlationId);
        activity?.SetTag("request.id", requestId);
        activity?.SetTag("http.method", context.Request.Method);
        activity?.SetTag("http.route", context.Request.Path.Value);

        await next(context).ConfigureAwait(false);
    }

    private static string? ResolveHeader(HttpContext context, string headerName)
    {
        if (!context.Request.Headers.TryGetValue(headerName, out var values))
        {
            return null;
        }

        var value = values.ToString();
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}

public sealed class RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context, ICorrelationContext correlationContext)
    {
        var started = Stopwatch.GetTimestamp();
        var method = context.Request.Method;
        var path = LogSanitizer.MaskSensitiveText(context.Request.Path.Value) ?? "/";

        using (CommerceLogging.BeginOperationScope(
            logger,
            correlationContext,
            "http.request.start",
            ("Method", method),
            ("Path", path)))
        {
            logger.LogInformation("HTTP {Method} {Path} started.", method, path);
        }

        try
        {
            await next(context).ConfigureAwait(false);
        }
        finally
        {
            var elapsedMs = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
            CommerceMetrics.HttpRequests.Add(
                1,
                new KeyValuePair<string, object?>("method", method),
                new KeyValuePair<string, object?>("status_code", context.Response.StatusCode));

            using (CommerceLogging.BeginOperationScope(
                logger,
                correlationContext,
                "http.request.complete",
                ("Method", method),
                ("Path", path),
                ("StatusCode", context.Response.StatusCode),
                ("ElapsedMs", elapsedMs)))
            {
                logger.LogInformation(
                    "HTTP {Method} {Path} completed with {StatusCode} in {ElapsedMs:F1}ms.",
                    method,
                    path,
                    context.Response.StatusCode,
                    elapsedMs);
            }
        }
    }
}
