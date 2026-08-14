using Commerce.Orders.Contracts.Orders;
using Commerce.Orders.Domain.Enums;
using Commerce.Framework.Data.Db;
using Microsoft.EntityFrameworkCore;

namespace Commerce.Orders.Infrastructure.Persistence.Repositories;

public sealed class OrderPurchaseVerifier(CommerceDbContext dbContext) : IOrderPurchaseVerifier
{
    public Task<bool> HasCustomerPurchasedProductAsync(
        int customerId,
        int productId,
        int storeId,
        CancellationToken cancellationToken = default) =>
        dbContext.Set<Commerce.Orders.Domain.Entities.Order>()
            .AsNoTracking()
            .Where(x =>
                x.CustomerId == customerId &&
                x.StoreId == storeId &&
                x.PaymentStatus == PaymentStatus.Paid)
            .SelectMany(x => x.Items)
            .AnyAsync(x => x.ProductId == productId, cancellationToken);
}
