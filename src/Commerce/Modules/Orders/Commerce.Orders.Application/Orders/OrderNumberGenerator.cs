using Commerce.Orders.Application.Abstractions;
using Commerce.Orders.Domain.Entities;

namespace Commerce.Orders.Application.Orders;

public interface IOrderNumberGenerator
{
    Task<string> GenerateAsync(int storeId, CancellationToken cancellationToken = default);
}

public sealed class OrderNumberGenerator(IOrderNumberSequenceRepository sequenceRepository) : IOrderNumberGenerator
{
    public async Task<string> GenerateAsync(int storeId, CancellationToken cancellationToken = default)
    {
        var year = DateTime.UtcNow.Year;
        var sequence = await sequenceRepository.GetOrCreateAsync(storeId, year, cancellationToken).ConfigureAwait(false);
        var next = sequence.Next();
        await sequenceRepository.SaveAsync(sequence, cancellationToken).ConfigureAwait(false);
        return $"ORD-{year}-{next:D6}";
    }
}

public interface IOrderAccessTokenGenerator
{
    string GenerateToken();
}

public sealed class OrderAccessTokenGenerator : IOrderAccessTokenGenerator
{
    public string GenerateToken()
    {
        Span<byte> bytes = stackalloc byte[32];
        System.Security.Cryptography.RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
