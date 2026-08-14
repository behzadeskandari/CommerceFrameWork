using Commerce.Pricing.Domain.Entities;
using Commerce.Pricing.Domain.Enums;

namespace Commerce.Pricing.Application.Abstractions;

public interface IPricingRepository
{
    Task<IReadOnlyList<Discount>> GetActiveDiscountsAsync(
        int storeId,
        DateTime utcNow,
        CancellationToken cancellationToken = default);

    Task<Discount?> GetDiscountByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Discount>> ListDiscountsAsync(CancellationToken cancellationToken = default);

    Task AddDiscountAsync(Discount discount, CancellationToken cancellationToken = default);

    Task SaveDiscountAsync(Discount discount, CancellationToken cancellationToken = default);

    Task<Coupon?> GetCouponByCodeAsync(string normalizedCode, CancellationToken cancellationToken = default);

    Task<Coupon?> GetCouponByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Coupon>> ListCouponsAsync(CancellationToken cancellationToken = default);

    Task AddCouponAsync(Coupon coupon, CancellationToken cancellationToken = default);

    Task SaveCouponAsync(Coupon coupon, CancellationToken cancellationToken = default);

    Task<int> GetCustomerCouponUsageCountAsync(
        int couponId,
        int customerId,
        CancellationToken cancellationToken = default);

    Task<bool> TryConsumeCouponUsageAsync(
        int couponId,
        int orderId,
        int? customerId,
        int? globalUsageLimit,
        int? perCustomerUsageLimit,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CustomerGroup>> ListCustomerGroupsAsync(int? storeId, CancellationToken cancellationToken = default);

    Task<CustomerGroup?> GetCustomerGroupAsync(int id, CancellationToken cancellationToken = default);

    Task AddCustomerGroupAsync(CustomerGroup group, CancellationToken cancellationToken = default);

    Task SaveCustomerGroupAsync(CustomerGroup group, CancellationToken cancellationToken = default);

    Task DeleteCustomerGroupAsync(CustomerGroup group, CancellationToken cancellationToken = default);

    Task<CustomerGroupPrice?> GetCustomerGroupPriceAsync(
        int customerGroupId,
        int storeId,
        int productId,
        int? variantId,
        int currencyId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CustomerGroupPrice>> ListCustomerGroupPricesAsync(int customerGroupId, CancellationToken cancellationToken = default);

    Task AddCustomerGroupPriceAsync(CustomerGroupPrice price, CancellationToken cancellationToken = default);

    Task SaveCustomerGroupPriceAsync(CustomerGroupPrice price, CancellationToken cancellationToken = default);

    Task DeleteCustomerGroupPriceAsync(CustomerGroupPrice price, CancellationToken cancellationToken = default);

    Task<CustomerGroupPrice?> GetCustomerGroupPriceByIdAsync(int id, CancellationToken cancellationToken = default);
}

public interface IProductCategoryLookup
{
    Task<IReadOnlyDictionary<int, IReadOnlyList<int>>> GetCategoryIdsByProductIdsAsync(
        IReadOnlyCollection<int> productIds,
        CancellationToken cancellationToken = default);
}
