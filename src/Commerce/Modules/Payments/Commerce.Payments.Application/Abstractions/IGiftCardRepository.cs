using Commerce.Payments.Domain.Entities;

namespace Commerce.Payments.Application.Abstractions;

public interface IGiftCardRepository
{
    Task<GiftCard?> GetByIdWithTransactionsAsync(int id, CancellationToken cancellationToken = default);

    Task<GiftCard?> GetByCodeWithTransactionsAsync(string code, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GiftCard>> ListAsync(int? storeId, CancellationToken cancellationToken = default);

    Task AddAsync(GiftCard giftCard, CancellationToken cancellationToken = default);

    Task UpdateAsync(GiftCard giftCard, CancellationToken cancellationToken = default);

    Task<bool> TryRedeemAsync(
        int giftCardId,
        decimal amount,
        int orderId,
        string idempotencyKey,
        CancellationToken cancellationToken = default);
}
