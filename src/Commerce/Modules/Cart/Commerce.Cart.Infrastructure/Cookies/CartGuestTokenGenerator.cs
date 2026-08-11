using System.Security.Cryptography;
using Commerce.Cart.Application.Abstractions;

namespace Commerce.Cart.Infrastructure.Cookies;

public sealed class CartGuestTokenGenerator : ICartGuestTokenGenerator
{
    public string GenerateToken()
    {
        Span<byte> bytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
