using System.Security.Cryptography;
using System.Text;
using Commerce.Integration.Contracts.Webhooks;

namespace Commerce.Integration.Application.Webhooks;

public sealed class WebhookSignatureService : IWebhookSignatureService
{
    public string ComputeSignature(string secret, long timestampUnix, string payload)
    {
        var signedPayload = $"{timestampUnix}.{payload}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(signedPayload));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public bool VerifySignature(string secret, long timestampUnix, string payload, string signatureHeader)
    {
        if (string.IsNullOrWhiteSpace(signatureHeader))
        {
            return false;
        }

        var expected = ComputeSignature(secret, timestampUnix, payload);
        var parts = signatureHeader.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var part in parts)
        {
            if (!part.StartsWith("v1=", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var provided = part[3..];
            if (FixedTimeEquals(provided, expected))
            {
                return true;
            }
        }

        return false;
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
}
