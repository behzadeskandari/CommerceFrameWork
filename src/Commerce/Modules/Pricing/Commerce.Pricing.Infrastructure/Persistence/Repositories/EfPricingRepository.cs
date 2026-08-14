using Commerce.Framework.Data.Db;
using Commerce.Pricing.Application.Abstractions;
using Commerce.Pricing.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Commerce.Pricing.Infrastructure.Persistence.Repositories;

public sealed class EfPricingRepository(CommerceDbContext dbContext) : IPricingRepository
{
    public async Task<IReadOnlyList<Discount>> GetActiveDiscountsAsync(
        int storeId,
        DateTime utcNow,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Set<Discount>()
            .AsNoTracking()
            .Include(x => x.Targets)
            .Where(x => !x.IsDeleted && x.IsActive)
            .Where(x => !x.StartsAtUtc.HasValue || x.StartsAtUtc.Value <= utcNow)
            .Where(x => !x.EndsAtUtc.HasValue || x.EndsAtUtc.Value >= utcNow)
            .Where(x => !x.StoreId.HasValue || x.StoreId.Value == storeId)
            .OrderByDescending(x => x.Priority)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public Task<Discount?> GetDiscountByIdAsync(int id, CancellationToken cancellationToken = default) =>
        dbContext.Set<Discount>()
            .Include(x => x.Targets)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<IReadOnlyList<Discount>> ListDiscountsAsync(CancellationToken cancellationToken = default) =>
        dbContext.Set<Discount>()
            .AsNoTracking()
            .Include(x => x.Targets)
            .OrderByDescending(x => x.UpdatedAtUtc)
            .ToListAsync(cancellationToken)
            .ContinueWith(t => (IReadOnlyList<Discount>)t.Result, cancellationToken);

    public async Task AddDiscountAsync(Discount discount, CancellationToken cancellationToken = default)
    {
        dbContext.Set<Discount>().Add(discount);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task SaveDiscountAsync(Discount discount, CancellationToken cancellationToken = default)
    {
        dbContext.Set<Discount>().Update(discount);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public Task<Coupon?> GetCouponByCodeAsync(string normalizedCode, CancellationToken cancellationToken = default) =>
        dbContext.Set<Coupon>()
            .FirstOrDefaultAsync(x => x.Code == normalizedCode && !x.IsDeleted, cancellationToken);

    public Task<Coupon?> GetCouponByIdAsync(int id, CancellationToken cancellationToken = default) =>
        dbContext.Set<Coupon>().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<IReadOnlyList<Coupon>> ListCouponsAsync(CancellationToken cancellationToken = default) =>
        dbContext.Set<Coupon>()
            .AsNoTracking()
            .OrderByDescending(x => x.UpdatedAtUtc)
            .ToListAsync(cancellationToken)
            .ContinueWith(t => (IReadOnlyList<Coupon>)t.Result, cancellationToken);

    public async Task AddCouponAsync(Coupon coupon, CancellationToken cancellationToken = default)
    {
        dbContext.Set<Coupon>().Add(coupon);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task SaveCouponAsync(Coupon coupon, CancellationToken cancellationToken = default)
    {
        dbContext.Set<Coupon>().Update(coupon);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public Task<int> GetCustomerCouponUsageCountAsync(
        int couponId,
        int customerId,
        CancellationToken cancellationToken = default) =>
        dbContext.Set<CouponUsage>()
            .CountAsync(x => x.CouponId == couponId && x.CustomerId == customerId, cancellationToken);

    public async Task<bool> TryConsumeCouponUsageAsync(
        int couponId,
        int orderId,
        int? customerId,
        int? globalUsageLimit,
        int? perCustomerUsageLimit,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        var coupon = await dbContext.Set<Coupon>()
            .FirstOrDefaultAsync(x => x.Id == couponId && !x.IsDeleted, cancellationToken)
            .ConfigureAwait(false);

        if (coupon is null)
        {
            return false;
        }

        if (globalUsageLimit.HasValue && coupon.UsageCount >= globalUsageLimit.Value)
        {
            return false;
        }

        if (customerId.HasValue && perCustomerUsageLimit.HasValue)
        {
            var customerUsage = await dbContext.Set<CouponUsage>()
                .CountAsync(x => x.CouponId == couponId && x.CustomerId == customerId.Value, cancellationToken)
                .ConfigureAwait(false);

            if (customerUsage >= perCustomerUsageLimit.Value)
            {
                return false;
            }
        }

        var usage = CouponUsage.Create(couponId, customerId, orderId);
        dbContext.Set<CouponUsage>().Add(usage);
        coupon.RecordUsage(orderId, customerId);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            return false;
        }
    }

    public Task<IReadOnlyList<CustomerGroup>> ListCustomerGroupsAsync(int? storeId, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Set<CustomerGroup>().AsNoTracking().AsQueryable();
        if (storeId.HasValue)
        {
            query = query.Where(x => x.StoreId == storeId.Value);
        }

        return query.OrderBy(x => x.DisplayOrder).ThenBy(x => x.Name)
            .ToListAsync(cancellationToken)
            .ContinueWith(t => (IReadOnlyList<CustomerGroup>)t.Result, cancellationToken);
    }

    public Task<CustomerGroup?> GetCustomerGroupAsync(int id, CancellationToken cancellationToken = default) =>
        dbContext.Set<CustomerGroup>().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task AddCustomerGroupAsync(CustomerGroup group, CancellationToken cancellationToken = default)
    {
        dbContext.Set<CustomerGroup>().Add(group);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task SaveCustomerGroupAsync(CustomerGroup group, CancellationToken cancellationToken = default)
    {
        dbContext.Set<CustomerGroup>().Update(group);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteCustomerGroupAsync(CustomerGroup group, CancellationToken cancellationToken = default)
    {
        dbContext.Set<CustomerGroup>().Remove(group);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public Task<CustomerGroupPrice?> GetCustomerGroupPriceAsync(
        int customerGroupId,
        int storeId,
        int productId,
        int? variantId,
        int currencyId,
        CancellationToken cancellationToken = default) =>
        dbContext.Set<CustomerGroupPrice>()
            .AsNoTracking()
            .Where(x => x.CustomerGroupId == customerGroupId && x.StoreId == storeId && x.ProductId == productId && x.CurrencyId == currencyId && x.IsActive)
            .Where(x => x.VariantId == variantId)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<CustomerGroupPrice?> GetCustomerGroupPriceByIdAsync(int id, CancellationToken cancellationToken = default) =>
        dbContext.Set<CustomerGroupPrice>().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<IReadOnlyList<CustomerGroupPrice>> ListCustomerGroupPricesAsync(int customerGroupId, CancellationToken cancellationToken = default) =>
        dbContext.Set<CustomerGroupPrice>()
            .AsNoTracking()
            .Where(x => x.CustomerGroupId == customerGroupId)
            .OrderByDescending(x => x.UpdatedAtUtc)
            .ToListAsync(cancellationToken)
            .ContinueWith(t => (IReadOnlyList<CustomerGroupPrice>)t.Result, cancellationToken);

    public async Task AddCustomerGroupPriceAsync(CustomerGroupPrice price, CancellationToken cancellationToken = default)
    {
        dbContext.Set<CustomerGroupPrice>().Add(price);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task SaveCustomerGroupPriceAsync(CustomerGroupPrice price, CancellationToken cancellationToken = default)
    {
        dbContext.Set<CustomerGroupPrice>().Update(price);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteCustomerGroupPriceAsync(CustomerGroupPrice price, CancellationToken cancellationToken = default)
    {
        dbContext.Set<CustomerGroupPrice>().Remove(price);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
