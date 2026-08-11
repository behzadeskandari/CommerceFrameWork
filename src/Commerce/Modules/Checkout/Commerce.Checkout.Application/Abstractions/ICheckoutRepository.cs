using Commerce.Checkout.Domain.Entities;

namespace Commerce.Checkout.Application.Abstractions;

public interface ICheckoutRepository
{
    Task<CheckoutSession?> GetByIdWithItemsAsync(int checkoutId, CancellationToken cancellationToken = default);

    Task<CheckoutSession?> GetActiveByCartIdAsync(int cartId, CancellationToken cancellationToken = default);

    Task AddAsync(CheckoutSession session, CancellationToken cancellationToken = default);

    Task SaveAsync(CheckoutSession session, CancellationToken cancellationToken = default);
}
