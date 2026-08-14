namespace Commerce.Plugin.Payment.Stripe;

internal static class StripeSettingKeys
{
    public const string SecretKey = "Payment.Stripe.SecretKey";
    public const string WebhookSecret = "Payment.Stripe.WebhookSecret";
    public const string Sandbox = "Payment.Stripe.Sandbox";
}

internal static class StripeEndpoints
{
    public const string ApiBase = "https://api.stripe.com/v1";
}

internal static class StripeMetadataKeys
{
    public const string PaymentId = "commerce_payment_id";
    public const string OrderId = "commerce_order_id";
    public const string StoreId = "commerce_store_id";
    public const string IdempotencyKey = "commerce_idempotency_key";
}
