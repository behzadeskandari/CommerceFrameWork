using Commerce.Framework.Scheduling;
using Commerce.Observability.Application.Logging;
using Xunit;

namespace Commerce.Tests.Unit.Observability;

public sealed class Phase38ObservabilityTests
{
    [Fact]
    public void LogSanitizer_MasksSensitiveKeysAndBearerTokens()
    {
        Assert.Equal("***", LogSanitizer.SanitizeValue("password", "secret123"));
        Assert.Equal("Authorization: Bearer ***", LogSanitizer.MaskSensitiveText("Authorization: Bearer abc.def.ghi"));
    }

    [Fact]
    public void JobObservabilityPayload_RoundTripsCorrelationId()
    {
        var payload = JobObservabilityPayload.EnrichPayload("{\"orderId\":42}", "corr-123");
        Assert.Equal("corr-123", JobObservabilityPayload.ExtractCorrelationId(payload));
    }

    [Fact]
    public void CommerceMetrics_RegistersCommerceMeter()
    {
        Assert.Equal("Commerce", Commerce.Framework.Application.Observability.CommerceMetrics.Meter.Name);
        Assert.NotNull(Commerce.Framework.Application.Observability.CommerceTracing.Source);
    }
}
