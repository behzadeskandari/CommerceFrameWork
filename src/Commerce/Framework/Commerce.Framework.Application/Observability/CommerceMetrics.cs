using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Commerce.Framework.Application.Observability;

public static class CommerceMetrics
{
    public static readonly Meter Meter = new("Commerce", "1.0.0");
    public static readonly Counter<long> HttpRequests = Meter.CreateCounter<long>("commerce.http.requests");
    public static readonly Counter<long> CartOperations = Meter.CreateCounter<long>("commerce.cart.operations");
    public static readonly Counter<long> CheckoutOperations = Meter.CreateCounter<long>("commerce.checkout.operations");
    public static readonly Counter<long> PaymentOperations = Meter.CreateCounter<long>("commerce.payment.operations");
    public static readonly Counter<long> OrderOperations = Meter.CreateCounter<long>("commerce.order.operations");
    public static readonly Counter<long> NotificationOperations = Meter.CreateCounter<long>("commerce.notification.operations");
}

public static class CommerceTracing
{
    public static readonly ActivitySource Source = new("Commerce", "1.0.0");

    public static Activity? StartCommerceActivity(string name, string? correlationId = null)
    {
        var activity = Source.StartActivity(name);
        if (activity is not null && !string.IsNullOrWhiteSpace(correlationId))
        {
            activity.SetTag("correlation.id", correlationId);
        }

        return activity;
    }
}
