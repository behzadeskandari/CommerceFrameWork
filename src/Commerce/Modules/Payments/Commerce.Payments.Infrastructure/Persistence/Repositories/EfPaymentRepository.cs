using Commerce.Framework.Data.Db;

using Commerce.Payments.Application.Abstractions;

using Commerce.Payments.Domain.Entities;

using Commerce.Payments.Domain.Enums;

using Microsoft.EntityFrameworkCore;



namespace Commerce.Payments.Infrastructure.Persistence.Repositories;



public sealed class EfPaymentRepository(CommerceDbContext dbContext) : IPaymentRepository

{

    public Task<Payment?> GetByIdWithDetailsAsync(int paymentId, CancellationToken cancellationToken = default) =>

        dbContext.Set<Payment>()

            .Include(x => x.Transactions)

            .Include(x => x.Attempts)

            .Include(x => x.Refunds)

            .ThenInclude(x => x.Transactions)

            .FirstOrDefaultAsync(x => x.Id == paymentId, cancellationToken);



    public Task<Payment?> GetByOrderIdAsync(int orderId, CancellationToken cancellationToken = default) =>

        dbContext.Set<Payment>()

            .Include(x => x.Transactions)

            .Include(x => x.Attempts)

            .Include(x => x.Refunds)

            .ThenInclude(x => x.Transactions)

            .FirstOrDefaultAsync(x => x.OrderId == orderId, cancellationToken);



    public Task<Payment?> GetByIdempotencyKeyAsync(int storeId, string idempotencyKey, CancellationToken cancellationToken = default) =>

        dbContext.Set<Payment>()

            .Include(x => x.Transactions)

            .Include(x => x.Attempts)

            .Include(x => x.Refunds)

            .FirstOrDefaultAsync(x => x.StoreId == storeId && x.IdempotencyKey == idempotencyKey, cancellationToken);



    public async Task<(IReadOnlyList<Payment> Items, int TotalCount)> ListAsync(

        PaymentListCriteria criteria,

        CancellationToken cancellationToken = default)

    {

        var query = dbContext.Set<Payment>().AsNoTracking().AsQueryable();



        if (criteria.StoreId.HasValue)

        {

            query = query.Where(x => x.StoreId == criteria.StoreId.Value);

        }



        if (criteria.OrderId.HasValue)

        {

            query = query.Where(x => x.OrderId == criteria.OrderId.Value);

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



    public async Task AddAsync(Payment payment, CancellationToken cancellationToken = default)

    {

        dbContext.Set<Payment>().Add(payment);

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

    }



    public async Task SaveAsync(Payment payment, CancellationToken cancellationToken = default)

    {

        dbContext.Set<Payment>().Update(payment);

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

    }



    public Task<IReadOnlyList<PaymentMethod>> GetActiveMethodsAsync(int storeId, CancellationToken cancellationToken = default) =>

        dbContext.Set<PaymentMethod>()

            .AsNoTracking()

            .Where(x => x.StoreId == storeId && x.IsActive && !x.IsDeleted)

            .OrderBy(x => x.DisplayOrder)

            .ToListAsync(cancellationToken)

            .ContinueWith(t => (IReadOnlyList<PaymentMethod>)t.Result, cancellationToken);



    public Task<PaymentMethod?> GetMethodByIdAsync(int id, CancellationToken cancellationToken = default) =>

        dbContext.Set<PaymentMethod>().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);



    public Task<PaymentMethod?> GetMethodBySystemNameAsync(int storeId, string systemName, CancellationToken cancellationToken = default) =>

        dbContext.Set<PaymentMethod>()

            .FirstOrDefaultAsync(

                x => x.StoreId == storeId &&

                     x.SystemName == systemName.Trim().ToLowerInvariant() &&

                     !x.IsDeleted,

                cancellationToken);



    public Task<IReadOnlyList<PaymentMethod>> ListMethodsAsync(int? storeId, CancellationToken cancellationToken = default)

    {

        var query = dbContext.Set<PaymentMethod>().AsNoTracking().AsQueryable();

        if (storeId.HasValue)

        {

            query = query.Where(x => x.StoreId == storeId.Value);

        }



        return query.OrderBy(x => x.DisplayOrder).ToListAsync(cancellationToken)

            .ContinueWith(t => (IReadOnlyList<PaymentMethod>)t.Result, cancellationToken);

    }



    public async Task AddMethodAsync(PaymentMethod method, CancellationToken cancellationToken = default)

    {

        dbContext.Set<PaymentMethod>().Add(method);

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

    }



    public async Task SaveMethodAsync(PaymentMethod method, CancellationToken cancellationToken = default)

    {

        dbContext.Set<PaymentMethod>().Update(method);

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

    }



    public Task<PaymentCallbackRecord?> GetCallbackRecordAsync(

        string providerSystemName,

        string callbackKey,

        CancellationToken cancellationToken = default) =>

        dbContext.Set<PaymentCallbackRecord>()

            .FirstOrDefaultAsync(

                x => x.ProviderSystemName == providerSystemName && x.CallbackKey == callbackKey,

                cancellationToken);



    public async Task AddCallbackRecordAsync(PaymentCallbackRecord record, CancellationToken cancellationToken = default)

    {

        dbContext.Set<PaymentCallbackRecord>().Add(record);

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

    }

    public Task<Refund?> GetRefundByIdempotencyKeyAsync(
        int paymentId,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        dbContext.Set<Refund>()
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.PaymentId == paymentId && x.IdempotencyKey == idempotencyKey,
                cancellationToken);
}

