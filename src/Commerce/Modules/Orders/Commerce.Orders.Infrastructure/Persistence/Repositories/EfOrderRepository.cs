using Commerce.Orders.Application.Abstractions;
using Commerce.Orders.Contracts.Orders;
using Commerce.Orders.Domain.Entities;
using Commerce.Orders.Domain.Enums;
using Commerce.Framework.Data.Db;
using Microsoft.EntityFrameworkCore;

namespace Commerce.Orders.Infrastructure.Persistence.Repositories;

public sealed class EfOrderRepository(CommerceDbContext dbContext) : IOrderRepository, IOrderPaymentSyncRepository
{
    Task<Order?> IOrderPaymentSyncRepository.GetByIdAsync(int orderId, CancellationToken cancellationToken) =>
        GetByIdWithDetailsAsync(orderId, cancellationToken);
    public Task<Order?> GetByIdWithDetailsAsync(int orderId, CancellationToken cancellationToken = default) =>
        dbContext.Set<Order>()
            .Include(x => x.Items)
            .Include(x => x.StatusHistory)
            .FirstOrDefaultAsync(x => x.Id == orderId, cancellationToken);

    public Task<Order?> GetByOrderNumberAsync(string orderNumber, CancellationToken cancellationToken = default) =>
        dbContext.Set<Order>()
            .Include(x => x.Items)
            .Include(x => x.StatusHistory)
            .FirstOrDefaultAsync(x => x.OrderNumber == orderNumber, cancellationToken);

    public Task<Order?> GetByCheckoutIdAsync(int checkoutId, CancellationToken cancellationToken = default) =>
        dbContext.Set<Order>()
            .FirstOrDefaultAsync(x => x.CheckoutId == checkoutId, cancellationToken);

    public async Task AddAsync(Order order, CancellationToken cancellationToken = default)
    {
        dbContext.Set<Order>().Add(order);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task SaveAsync(Order order, CancellationToken cancellationToken = default)
    {
        dbContext.Set<Order>().Update(order);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<(IReadOnlyList<Order> Items, int TotalCount)> ListAsync(
        OrderListCriteria criteria,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Set<Order>().AsQueryable();

        if (criteria.StoreId.HasValue)
        {
            query = query.Where(x => x.StoreId == criteria.StoreId.Value);
        }

        if (criteria.CustomerId.HasValue)
        {
            query = query.Where(x => x.CustomerId == criteria.CustomerId.Value);
        }

        if (!string.IsNullOrWhiteSpace(criteria.OrderNumber))
        {
            query = query.Where(x => x.OrderNumber.Contains(criteria.OrderNumber));
        }

        if (!string.IsNullOrWhiteSpace(criteria.Email))
        {
            var email = criteria.Email.Trim();
            query = query.Where(x =>
                (x.CustomerEmail != null && x.CustomerEmail.Contains(email)) ||
                (x.GuestEmail != null && x.GuestEmail.Contains(email)));
        }

        if (criteria.Status.HasValue)
        {
            query = query.Where(x => x.Status == criteria.Status.Value);
        }

        if (criteria.CreatedFromUtc.HasValue)
        {
            query = query.Where(x => x.CreatedAtUtc >= criteria.CreatedFromUtc.Value);
        }

        if (criteria.CreatedToUtc.HasValue)
        {
            query = query.Where(x => x.CreatedAtUtc <= criteria.CreatedToUtc.Value);
        }

        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var items = await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .Skip((criteria.Page - 1) * criteria.PageSize)
            .Take(criteria.PageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return (items, total);
    }
}

public sealed class EfOrderNumberSequenceRepository(CommerceDbContext dbContext) : IOrderNumberSequenceRepository
{
    public async Task<StoreOrderNumberSequence> GetOrCreateAsync(
        int storeId,
        int year,
        CancellationToken cancellationToken = default)
    {
        var existing = await dbContext.Set<StoreOrderNumberSequence>()
            .FirstOrDefaultAsync(x => x.StoreId == storeId && x.Year == year, cancellationToken)
            .ConfigureAwait(false);

        if (existing is not null)
        {
            return existing;
        }

        var sequence = StoreOrderNumberSequence.Create(storeId, year);
        dbContext.Set<StoreOrderNumberSequence>().Add(sequence);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return sequence;
    }

    public async Task SaveAsync(StoreOrderNumberSequence sequence, CancellationToken cancellationToken = default)
    {
        dbContext.Set<StoreOrderNumberSequence>().Update(sequence);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

public sealed class EfOrderCreationIdempotencyRepository(CommerceDbContext dbContext) : IOrderCreationIdempotencyRepository
{
    public Task<OrderCreationIdempotency?> GetByKeyAsync(
        int storeId,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        dbContext.Set<OrderCreationIdempotency>()
            .FirstOrDefaultAsync(
                x => x.StoreId == storeId && x.IdempotencyKey == idempotencyKey,
                cancellationToken);

    public async Task AddAsync(OrderCreationIdempotency record, CancellationToken cancellationToken = default)
    {
        dbContext.Set<OrderCreationIdempotency>().Add(record);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
