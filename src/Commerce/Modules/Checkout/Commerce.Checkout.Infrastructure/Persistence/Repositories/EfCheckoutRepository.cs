using Commerce.Checkout.Application.Abstractions;
using Commerce.Checkout.Domain.Entities;
using Commerce.Checkout.Domain.Enums;
using Commerce.Framework.Data.Db;
using Microsoft.EntityFrameworkCore;

namespace Commerce.Checkout.Infrastructure.Persistence.Repositories;

public sealed class EfCheckoutRepository(CommerceDbContext dbContext) : ICheckoutRepository
{
    public async Task<CheckoutSession?> GetByIdWithItemsAsync(int checkoutId, CancellationToken cancellationToken = default) =>
        await dbContext.Set<CheckoutSession>()
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == checkoutId, cancellationToken)
            .ConfigureAwait(false);

    public async Task<CheckoutSession?> GetActiveByCartIdAsync(int cartId, CancellationToken cancellationToken = default) =>
        await dbContext.Set<CheckoutSession>()
            .Include(x => x.Items)
            .Where(x =>
                x.CartId == cartId &&
                (x.Status == CheckoutStatus.Active ||
                 x.Status == CheckoutStatus.RequiresReview ||
                 x.Status == CheckoutStatus.ReadyForOrder) &&
                x.ExpiresAtUtc > DateTime.UtcNow)
            .OrderByDescending(x => x.UpdatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task AddAsync(CheckoutSession session, CancellationToken cancellationToken = default)
    {
        dbContext.Set<CheckoutSession>().Add(session);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task SaveAsync(CheckoutSession session, CancellationToken cancellationToken = default)
    {
        dbContext.Set<CheckoutSession>().Update(session);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
