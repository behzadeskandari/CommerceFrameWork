using System.Net;
using System.Text;
using Commerce.Payments.Contracts.Payments;
using Commerce.Payments.Domain.Enums;
using Commerce.Plugin.Payment.Stripe;
using Commerce.Plugin.Payment.ZarinPal;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Commerce.Tests.Unit.PaymentProviders;

public sealed class Phase35PaymentProviderTests
{
    [Fact]
    public async Task ZarinPal_CreatePayment_ReturnsRedirect_WhenRequestSucceeds()
    {
        var handler = new StubHttpHandler(_ =>
            JsonResponse("""
                {"data":{"code":100,"authority":"A000000000000000000000000000000000","message":"Success"},"errors":[]}
                """));

        var provider = CreateZarinPalProvider(handler, merchantId: "11111111-1111-1111-1111-111111111111");

        var result = await provider.CreatePaymentAsync(new PaymentRequest(
            1, 1, 10, null, "IRR", 10000m, "zarinpal",
            ReturnUrl: "https://shop.test/return",
            CancelUrl: "https://shop.test/cancel",
            IdempotencyKey: "pay-key-1"));

        Assert.True(result.Success);
        Assert.Equal(PaymentStatus.RedirectRequired, result.Status);
        Assert.Contains("A000000000000000000000000000000000", result.RedirectUrl, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ZarinPal_VerifyPayment_DoesNotTrustBrowserOkWithoutApiVerify()
    {
        var handler = new StubHttpHandler(request =>
        {
            if (request.RequestUri!.AbsolutePath.Contains("verify.json", StringComparison.Ordinal))
            {
                return JsonResponse("""
                    {"data":{"code":-51,"message":"Invalid authority"},"errors":[]}
                    """, HttpStatusCode.OK);
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        var provider = CreateZarinPalProvider(handler, merchantId: "11111111-1111-1111-1111-111111111111");

        var verification = await provider.VerifyPaymentAsync(
            1,
            "A000BAD",
            new Dictionary<string, string>
            {
                ["paymentId"] = "1",
                ["storeId"] = "1",
                ["amount"] = "10000",
                ["currency"] = "IRR",
                ["Status"] = "OK",
                ["Authority"] = "A000BAD"
            });

        Assert.False(verification.Success);
        Assert.Equal(PaymentStatus.Failed, verification.Status);
    }

    [Fact]
    public async Task ZarinPal_VerifyPayment_CapturesOnSuccessfulServerVerification()
    {
        var handler = new StubHttpHandler(request =>
        {
            if (request.RequestUri!.AbsolutePath.Contains("verify.json", StringComparison.Ordinal))
            {
                return JsonResponse("""
                    {"data":{"code":100,"ref_id":123456,"message":"Verified"},"errors":[]}
                    """);
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        var provider = CreateZarinPalProvider(handler, merchantId: "11111111-1111-1111-1111-111111111111");

        var verification = await provider.VerifyPaymentAsync(
            1,
            "A000GOOD",
            new Dictionary<string, string>
            {
                ["paymentId"] = "1",
                ["storeId"] = "1",
                ["amount"] = "10000",
                ["currency"] = "IRR",
                ["Status"] = "OK",
                ["Authority"] = "A000GOOD"
            });

        Assert.True(verification.Success);
        Assert.Equal(PaymentStatus.Captured, verification.Status);
        Assert.Equal("123456", verification.ProviderPaymentId);
    }

    [Fact]
    public async Task ZarinPal_VerifyPayment_CancelledWhenBrowserReportsNok()
    {
        var provider = CreateZarinPalProvider(new StubHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound)),
            merchantId: "11111111-1111-1111-1111-111111111111");

        var verification = await provider.VerifyPaymentAsync(
            1,
            "A000",
            new Dictionary<string, string>
            {
                ["paymentId"] = "1",
                ["storeId"] = "1",
                ["amount"] = "10000",
                ["currency"] = "IRR",
                ["Status"] = "NOK"
            });

        Assert.False(verification.Success);
        Assert.Equal(PaymentStatus.Cancelled, verification.Status);
    }

    [Fact]
    public async Task Stripe_CreatePayment_ReturnsRedirect_WhenSessionCreated()
    {
        var handler = new StubHttpHandler(_ =>
            JsonResponse("""
                {"id":"cs_test_123","url":"https://checkout.stripe.com/pay/cs_test_123","payment_intent":"pi_test_123"}
                """));

        var provider = CreateStripeProvider(handler, secretKey: "sk_test_123");

        var result = await provider.CreatePaymentAsync(new PaymentRequest(
            2, 1, 20, 5, "USD", 25.50m, "stripe",
            ReturnUrl: "https://shop.test/payment/success",
            CancelUrl: "https://shop.test/payment/cancel",
            IdempotencyKey: "stripe-key-1"));

        Assert.True(result.Success);
        Assert.Equal(PaymentStatus.RedirectRequired, result.Status);
        Assert.Equal("https://checkout.stripe.com/pay/cs_test_123", result.RedirectUrl);
    }

    [Fact]
    public async Task Stripe_VerifyPayment_CapturesWhenSessionPaid()
    {
        var handler = new StubHttpHandler(_ =>
            JsonResponse("""
                {"id":"cs_test_123","payment_status":"paid","status":"complete","payment_intent":"pi_test_123"}
                """));

        var provider = CreateStripeProvider(handler, secretKey: "sk_test_123");

        var verification = await provider.VerifyPaymentAsync(
            2,
            "pi_test_123",
            new Dictionary<string, string>
            {
                ["paymentId"] = "2",
                ["storeId"] = "1",
                ["session_id"] = "cs_test_123"
            });

        Assert.True(verification.Success);
        Assert.Equal(PaymentStatus.Captured, verification.Status);
    }

    [Fact]
    public void Stripe_WebhookSignature_RejectsInvalidSignature()
    {
        var payload = """{"id":"evt_test","type":"checkout.session.completed"}""";
        var valid = StripeApiClient.VerifyWebhookSignature(
            payload,
            "t=1700000000,v1=deadbeef",
            "whsec_test",
            out var error);

        Assert.False(valid);
        Assert.NotNull(error);
    }

    [Fact]
    public void Stripe_WebhookSignature_AcceptsValidSignature()
    {
        const string secret = "whsec_test_secret";
        const string payload = """{"id":"evt_test","type":"checkout.session.completed"}""";
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var signature = ComputeStripeSignature(secret, timestamp, payload);

        var valid = StripeApiClient.VerifyWebhookSignature(
            payload,
            $"t={timestamp},v1={signature}",
            secret,
            out var error);

        Assert.True(valid);
        Assert.Null(error);
    }

    [Fact]
    public void Stripe_MapSessionStatus_ReturnsUnknownForUnpaidSession()
    {
        var result = StripePaymentProvider.MapSessionStatus(
            new StripeSessionStatusResult(true, "unpaid", "open", "pi_test", null),
            "pi_test");

        Assert.False(result.Success);
        Assert.Equal("unknown_state", result.FailureCode);
        Assert.Equal(PaymentStatus.Initiated, result.Status);
    }

    private static ZarinPalPaymentProvider CreateZarinPalProvider(StubHttpHandler handler, string merchantId)
    {
        var client = new ZarinPalApiClient(new HttpClient(handler), NullLogger<ZarinPalApiClient>.Instance);
        var settings = new FakeSettings(new Dictionary<string, string>
        {
            ["Payment.ZarinPal.MerchantId"] = merchantId,
            ["Payment.ZarinPal.Sandbox"] = "true",
            ["Payment.ZarinPal.CallbackBaseUrl"] = "https://localhost/api/payments/callback/Payment.ZarinPal"
        });

        return new ZarinPalPaymentProvider(settings, client, NullLogger<ZarinPalPaymentProvider>.Instance);
    }

    private static StripePaymentProvider CreateStripeProvider(StubHttpHandler handler, string secretKey)
    {
        var client = new StripeApiClient(new HttpClient(handler), NullLogger<StripeApiClient>.Instance);
        var settings = new FakeSettings(new Dictionary<string, string>
        {
            ["Payment.Stripe.SecretKey"] = secretKey,
            ["Payment.Stripe.Sandbox"] = "true"
        });

        return new StripePaymentProvider(settings, client, NullLogger<StripePaymentProvider>.Instance);
    }

    private static HttpResponseMessage JsonResponse(string json, HttpStatusCode statusCode = HttpStatusCode.OK) =>
        new(statusCode)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

    private static string ComputeStripeSignature(string secret, long timestamp, string payload)
    {
        var signedPayload = $"{timestamp}.{payload}";
        using var hmac = new System.Security.Cryptography.HMACSHA256(Encoding.UTF8.GetBytes(secret));
        return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(signedPayload))).ToLowerInvariant();
    }

    private sealed class FakeSettings(Dictionary<string, string> values) : IPaymentProviderSettingsReader
    {
        public Task<string?> GetAsync(string key, int storeId, CancellationToken cancellationToken = default) =>
            Task.FromResult(values.TryGetValue(key, out var value) ? value : null);

        public Task<bool> GetBoolAsync(string key, int storeId, bool defaultValue = false, CancellationToken cancellationToken = default)
        {
            if (!values.TryGetValue(key, out var value))
            {
                return Task.FromResult(defaultValue);
            }

            return Task.FromResult(bool.TryParse(value, out var parsed) ? parsed : defaultValue);
        }
    }

    private sealed class StubHttpHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(responder(request));
    }
}
