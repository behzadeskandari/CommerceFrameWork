using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Commerce.Plugin.Payment.Stripe;

public sealed class StripeApiClient(HttpClient httpClient, ILogger<StripeApiClient> logger)
{
    public async Task<StripeCheckoutSessionResult> CreateCheckoutSessionAsync(
        string secretKey,
        long amountMinorUnits,
        string currency,
        string successUrl,
        string cancelUrl,
        IReadOnlyDictionary<string, string> metadata,
        string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var form = new List<KeyValuePair<string, string>>
        {
            new("mode", "payment"),
            new("success_url", successUrl),
            new("cancel_url", cancelUrl),
            new("line_items[0][quantity]", "1"),
            new("line_items[0][price_data][currency]", currency.ToLowerInvariant()),
            new("line_items[0][price_data][unit_amount]", amountMinorUnits.ToString()),
            new("line_items[0][price_data][product_data][name]", $"Order {metadata.GetValueOrDefault(StripeMetadataKeys.OrderId)}")
        };

        foreach (var pair in metadata)
        {
            form.Add(new KeyValuePair<string, string>($"metadata[{pair.Key}]", pair.Value));
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{StripeEndpoints.ApiBase}/checkout/sessions")
        {
            Content = new FormUrlEncodedContent(form)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", secretKey);
        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            request.Headers.Add("Idempotency-Key", idempotencyKey);
        }

        using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("Stripe checkout session failed: HTTP {Status} {Body}", (int)response.StatusCode, body);
            return new StripeCheckoutSessionResult(false, null, null, null, ParseStripeError(body));
        }

        using var json = JsonDocument.Parse(body);
        var root = json.RootElement;
        var sessionId = root.GetProperty("id").GetString();
        var url = root.GetProperty("url").GetString();
        var paymentIntentId = root.TryGetProperty("payment_intent", out var pi) && pi.ValueKind == JsonValueKind.String
            ? pi.GetString()
            : null;

        return new StripeCheckoutSessionResult(true, sessionId, url, paymentIntentId, null);
    }

    public async Task<StripeSessionStatusResult> GetCheckoutSessionAsync(
        string secretKey,
        string sessionId,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{StripeEndpoints.ApiBase}/checkout/sessions/{sessionId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", secretKey);

        using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("Stripe session retrieve failed: HTTP {Status} {Body}", (int)response.StatusCode, body);
            return new StripeSessionStatusResult(false, null, null, null, ParseStripeError(body));
        }

        using var json = JsonDocument.Parse(body);
        var root = json.RootElement;
        var paymentStatus = root.GetProperty("payment_status").GetString();
        var status = root.GetProperty("status").GetString();
        string? paymentIntentId = null;
        if (root.TryGetProperty("payment_intent", out var pi))
        {
            paymentIntentId = pi.ValueKind == JsonValueKind.String ? pi.GetString() : pi.GetProperty("id").GetString();
        }

        return new StripeSessionStatusResult(true, paymentStatus, status, paymentIntentId, null);
    }

    public async Task<StripeRefundResult> CreateRefundAsync(
        string secretKey,
        string paymentIntentId,
        long amountMinorUnits,
        string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var form = new List<KeyValuePair<string, string>>
        {
            new("payment_intent", paymentIntentId),
            new("amount", amountMinorUnits.ToString())
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{StripeEndpoints.ApiBase}/refunds")
        {
            Content = new FormUrlEncodedContent(form)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", secretKey);
        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            request.Headers.Add("Idempotency-Key", idempotencyKey);
        }

        using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            return new StripeRefundResult(false, null, ParseStripeError(body));
        }

        using var json = JsonDocument.Parse(body);
        var refundId = json.RootElement.GetProperty("id").GetString();
        return new StripeRefundResult(true, refundId, null);
    }

    public async Task<StripeCancelResult> CancelPaymentIntentAsync(
        string secretKey,
        string paymentIntentId,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{StripeEndpoints.ApiBase}/payment_intents/{paymentIntentId}/cancel");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", secretKey);

        using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            return new StripeCancelResult(false, ParseStripeError(body));
        }

        return new StripeCancelResult(true, null);
    }

    public async Task<StripeCaptureResult> CapturePaymentIntentAsync(
        string secretKey,
        string paymentIntentId,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{StripeEndpoints.ApiBase}/payment_intents/{paymentIntentId}/capture");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", secretKey);

        using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            return new StripeCaptureResult(false, ParseStripeError(body));
        }

        return new StripeCaptureResult(true, null);
    }

    public static bool VerifyWebhookSignature(string payload, string signatureHeader, string webhookSecret, out string? error)
    {
        error = null;
        if (string.IsNullOrWhiteSpace(signatureHeader))
        {
            error = "Missing Stripe-Signature header.";
            return false;
        }

        long timestamp = 0;
        string? signature = null;
        foreach (var part in signatureHeader.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (part.StartsWith("t=", StringComparison.Ordinal))
            {
                long.TryParse(part[2..], out timestamp);
            }
            else if (part.StartsWith("v1=", StringComparison.Ordinal))
            {
                signature = part[3..];
            }
        }

        if (timestamp <= 0 || string.IsNullOrWhiteSpace(signature))
        {
            error = "Invalid Stripe-Signature header.";
            return false;
        }

        var utcNow = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        if (Math.Abs(utcNow - timestamp) > 300)
        {
            error = "Stripe webhook timestamp outside tolerance.";
            return false;
        }

        var signedPayload = $"{timestamp}.{payload}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(webhookSecret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(signedPayload));
        var expected = Convert.ToHexString(hash).ToLowerInvariant();

        if (!FixedTimeEquals(expected, signature))
        {
            error = "Stripe webhook signature mismatch.";
            return false;
        }

        return true;
    }

    private static bool FixedTimeEquals(string a, string b)
    {
        if (a.Length != b.Length)
        {
            return false;
        }

        var result = 0;
        for (var i = 0; i < a.Length; i++)
        {
            result |= a[i] ^ b[i];
        }

        return result == 0;
    }

    private static string ParseStripeError(string body)
    {
        try
        {
            using var json = JsonDocument.Parse(body);
            if (json.RootElement.TryGetProperty("error", out var error) &&
                error.TryGetProperty("message", out var message))
            {
                return message.GetString() ?? body;
            }
        }
        catch (JsonException)
        {
        }

        return body;
    }
}

public sealed record StripeCheckoutSessionResult(
    bool Success,
    string? SessionId,
    string? Url,
    string? PaymentIntentId,
    string? ErrorMessage);

public sealed record StripeSessionStatusResult(
    bool Success,
    string? PaymentStatus,
    string? Status,
    string? PaymentIntentId,
    string? ErrorMessage);

public sealed record StripeRefundResult(bool Success, string? RefundId, string? ErrorMessage);

public sealed record StripeCancelResult(bool Success, string? ErrorMessage);

public sealed record StripeCaptureResult(bool Success, string? ErrorMessage);
