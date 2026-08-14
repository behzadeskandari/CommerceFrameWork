namespace Commerce.Framework.Contracts.Observability;

public interface ICorrelationContext
{
    string? CorrelationId { get; }

    string? RequestId { get; }

    string? TraceId { get; }
}

public static class ObservabilityHeaders
{
    public const string CorrelationId = "X-Correlation-ID";
    public const string RequestId = "X-Request-ID";
}
