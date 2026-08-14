using Commerce.Framework.Contracts.Observability;
using Microsoft.Extensions.Logging;

namespace Commerce.Framework.Application.Observability;

public static class CommerceLogging
{
    public static IDisposable BeginOperationScope(
        ILogger logger,
        ICorrelationContext correlation,
        string operation,
        params (string Key, object? Value)[] properties)
    {
        var state = new Dictionary<string, object?>
        {
            ["Operation"] = operation
        };

        if (!string.IsNullOrWhiteSpace(correlation.CorrelationId))
        {
            state["CorrelationId"] = correlation.CorrelationId;
        }

        if (!string.IsNullOrWhiteSpace(correlation.RequestId))
        {
            state["RequestId"] = correlation.RequestId;
        }

        foreach (var (key, value) in properties)
        {
            if (value is not null)
            {
                state[key] = value;
            }
        }

        return logger.BeginScope(state)!;
    }
}
