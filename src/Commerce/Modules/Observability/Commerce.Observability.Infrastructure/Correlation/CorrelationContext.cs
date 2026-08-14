using Commerce.Framework.Contracts.Observability;
using Microsoft.AspNetCore.Http;

namespace Commerce.Observability.Infrastructure.Correlation;

public sealed class HttpCorrelationContext(IHttpContextAccessor httpContextAccessor) : ICorrelationContext
{
    public string? CorrelationId =>
        httpContextAccessor.HttpContext?.Items[CorrelationContextKeys.CorrelationId] as string;

    public string? RequestId =>
        httpContextAccessor.HttpContext?.Items[CorrelationContextKeys.RequestId] as string;

    public string? TraceId =>
        System.Diagnostics.Activity.Current?.TraceId.ToString()
        ?? httpContextAccessor.HttpContext?.TraceIdentifier;
}

public sealed class JobCorrelationContext : ICorrelationContext
{
    public string? CorrelationId { get; private set; }

    public string? RequestId { get; private set; }

    public string? TraceId => System.Diagnostics.Activity.Current?.TraceId.ToString();

    public void Set(string? correlationId, string? requestId = null)
    {
        CorrelationId = correlationId;
        RequestId = requestId ?? correlationId;
    }
}

internal static class CorrelationContextKeys
{
    public const string CorrelationId = "Commerce.CorrelationId";
    public const string RequestId = "Commerce.RequestId";
}
