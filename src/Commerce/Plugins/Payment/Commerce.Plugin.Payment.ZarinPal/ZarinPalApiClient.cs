using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace Commerce.Plugin.Payment.ZarinPal;

public sealed class ZarinPalApiClient(HttpClient httpClient, ILogger<ZarinPalApiClient> logger)
{
    public async Task<ZarinPalRequestResult> RequestPaymentAsync(
        string apiBase,
        string merchantId,
        long amountInRials,
        string callbackUrl,
        string description,
        CancellationToken cancellationToken)
    {
        var payload = new
        {
            merchant_id = merchantId,
            amount = amountInRials,
            callback_url = callbackUrl,
            description
        };

        using var response = await httpClient
            .PostAsJsonAsync($"{apiBase}/request.json", payload, cancellationToken)
            .ConfigureAwait(false);

        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("ZarinPal request failed with HTTP {StatusCode}: {Body}", (int)response.StatusCode, body);
            return new ZarinPalRequestResult(false, null, "http_error", $"HTTP {(int)response.StatusCode}");
        }

        var parsed = JsonSerializer.Deserialize<ZarinPalEnvelope<ZarinPalRequestData>>(body);
        if (parsed?.Data?.Code == 100 && !string.IsNullOrWhiteSpace(parsed.Data.Authority))
        {
            return new ZarinPalRequestResult(true, parsed.Data.Authority, null, null);
        }

        var error = parsed?.Errors?.FirstOrDefault()?.Message ?? parsed?.Data?.Message ?? "ZarinPal request rejected.";
        return new ZarinPalRequestResult(false, null, parsed?.Data?.Code.ToString(), error);
    }

    public async Task<ZarinPalVerifyResult> VerifyPaymentAsync(
        string apiBase,
        string merchantId,
        long amountInRials,
        string authority,
        CancellationToken cancellationToken)
    {
        var payload = new
        {
            merchant_id = merchantId,
            amount = amountInRials,
            authority
        };

        using var response = await httpClient
            .PostAsJsonAsync($"{apiBase}/verify.json", payload, cancellationToken)
            .ConfigureAwait(false);

        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("ZarinPal verify failed with HTTP {StatusCode}: {Body}", (int)response.StatusCode, body);
            return new ZarinPalVerifyResult(false, false, null, "http_error", $"HTTP {(int)response.StatusCode}");
        }

        var parsed = JsonSerializer.Deserialize<ZarinPalEnvelope<ZarinPalVerifyData>>(body);
        var code = parsed?.Data?.Code ?? 0;
        if (code is 100 or 101)
        {
            return new ZarinPalVerifyResult(
                true,
                code == 101,
                parsed?.Data?.RefId?.ToString(),
                null,
                null);
        }

        return new ZarinPalVerifyResult(
            false,
            false,
            null,
            code.ToString(),
            parsed?.Errors?.FirstOrDefault()?.Message ?? parsed?.Data?.Message ?? "Verification failed.");
    }
}

public sealed record ZarinPalRequestResult(
    bool Success,
    string? Authority,
    string? FailureCode,
    string? FailureMessage);

public sealed record ZarinPalVerifyResult(
    bool Success,
    bool AlreadyVerified,
    string? RefId,
    string? FailureCode,
    string? FailureMessage);

internal sealed class ZarinPalEnvelope<T>
{
    [JsonPropertyName("data")]
    public T? Data { get; set; }

    [JsonPropertyName("errors")]
    public List<ZarinPalError>? Errors { get; set; }
}

internal sealed class ZarinPalRequestData
{
    [JsonPropertyName("authority")]
    public string? Authority { get; set; }

    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}

internal sealed class ZarinPalVerifyData
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("ref_id")]
    public long? RefId { get; set; }
}

internal sealed class ZarinPalError
{
    [JsonPropertyName("message")]
    public string? Message { get; set; }
}
