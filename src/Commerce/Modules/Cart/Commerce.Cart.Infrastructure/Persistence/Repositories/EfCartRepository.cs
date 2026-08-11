using Commerce.Cart.Application.Abstractions;
using Commerce.Cart.Domain.Entities;
using Commerce.Cart.Domain.Enums;
using Commerce.Framework.Data.Db;
using Microsoft.EntityFrameworkCore;

namespace Commerce.Cart.Infrastructure.Persistence.Repositories;

public sealed class EfCartRepository(CommerceDbContext dbContext) : ICartRepository
{
    public async Task<ShoppingCart?> GetByIdWithItemsAsync(int cartId, CancellationToken cancellationToken = default)
    {
        var cart = await dbContext.Set<ShoppingCart>()
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == cartId, cancellationToken)
            .ConfigureAwait(false);

        return cart;
    }

    public async Task<ShoppingCart?> GetActiveCustomerCartAsync(
        int storeId,
        int customerId,
        int currencyId,
        CancellationToken cancellationToken = default) =>
        await QueryActiveCarts(storeId, currencyId, cancellationToken)
            .Where(x => x.CustomerId == customerId)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task<ShoppingCart?> GetActiveGuestCartAsync(
        int storeId,
        string guestToken,
        int currencyId,
        CancellationToken cancellationToken = default) =>
        await QueryActiveCarts(storeId, currencyId, cancellationToken)
            .Where(x => x.GuestToken == guestToken)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task AddAsync(ShoppingCart cart, CancellationToken cancellationToken = default)
    {
        dbContext.Set<ShoppingCart>().Add(cart);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task SaveAsync(ShoppingCart cart, CancellationToken cancellationToken = default)
    {
        dbContext.Set<ShoppingCart>().Update(cart);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private IQueryable<ShoppingCart> QueryActiveCarts(int storeId, int currencyId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return dbContext.Set<ShoppingCart>()
            .Include(x => x.Items)
            .Where(x =>
                x.StoreId == storeId &&
                x.CurrencyId == currencyId &&
                x.Status == CartStatus.Active &&
                x.ExpiresAtUtc > DateTime.UtcNow);
    }
}
