using System.Security.Cryptography;
using System.Text;
using Commerce.Framework.Core.Results;
using Commerce.Host.Payments;
using Commerce.Payments.Contracts.Callbacks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Commerce.Host.Payments;

[ApiController]
[Route("api/payments/callback")]
public sealed class PaymentCallbackController(IPaymentCallbackDispatcher callbackDispatcher) : ControllerBase
{
    [HttpPost("{provider}")]
    [HttpGet("{provider}")]
    [AllowAnonymous]
    public async Task<IActionResult> Handle(string provider, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);

        var callbackData = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in Request.Query)
        {
            callbackData[pair.Key] = pair.Value.ToString();
        }

        if (!string.IsNullOrWhiteSpace(body))
        {
            callbackData["body"] = body;
        }

        var headers = Request.Headers
            .ToDictionary(x => x.Key, x => x.Value.ToString(), StringComparer.OrdinalIgnoreCase);

        var callbackKey = Request.Headers.TryGetValue("X-Callback-Key", out var headerKey) && !string.IsNullOrWhiteSpace(headerKey)
            ? headerKey.ToString()
            : ComputeHash($"{provider}:{body}");

        var payloadHash = ComputeHash(body);

        var context = new PaymentCallbackContext(
            provider,
            callbackKey,
            payloadHash,
            callbackData,
            headers);

        var result = await callbackDispatcher.DispatchAsync(context, cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            return PaymentActionResults.ToActionResult(this, result, _ => (object?)null);
        }

        if (result.Value!.Ignored)
        {
            return Ok(new { success = true, ignored = true });
        }

        return PaymentActionResults.ToActionResult(this, Result.Success(result.Value.Payment!), value => value);
    }

    private static string ComputeHash(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input ?? string.Empty));
        return Convert.ToHexString(bytes);
    }
}
