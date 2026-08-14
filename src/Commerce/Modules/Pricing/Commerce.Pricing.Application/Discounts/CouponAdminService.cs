using Commerce.Pricing.Application.Abstractions;
using Commerce.Pricing.Contracts.Discounts;
using Commerce.Pricing.Domain.Entities;
using Commerce.Framework.Core.Errors;
using Commerce.Framework.Core.Results;

namespace Commerce.Pricing.Application.Discounts;

public sealed class CouponAdminService(IPricingRepository repository) : ICouponAdminService
{
    public async Task<Result<IReadOnlyList<CouponSummaryDto>>> ListAsync(CancellationToken cancellationToken = default)
    {
        var coupons = await repository.ListCouponsAsync(cancellationToken).ConfigureAwait(false);
        var summaries = new List<CouponSummaryDto>();

        foreach (var coupon in coupons.Where(c => !c.IsDeleted))
        {
            var discount = await repository.GetDiscountByIdAsync(coupon.DiscountId, cancellationToken).ConfigureAwait(false);
            summaries.Add(MapSummary(coupon, discount?.Name ?? "Unknown"));
        }

        return Result.Success<IReadOnlyList<CouponSummaryDto>>(summaries);
    }

    public async Task<Result<CouponDetailDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var coupon = await repository.GetCouponByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (coupon is null || coupon.IsDeleted)
        {
            return Result.Failure<CouponDetailDto>(Error.NotFound($"Coupon '{id}' was not found."));
        }

        var discount = await repository.GetDiscountByIdAsync(coupon.DiscountId, cancellationToken).ConfigureAwait(false);
        return Result.Success(MapDetail(coupon, discount?.Name ?? "Unknown"));
    }

    public async Task<Result<CouponDetailDto>> CreateAsync(CreateCouponRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var discount = await repository.GetDiscountByIdAsync(request.DiscountId, cancellationToken).ConfigureAwait(false);
        if (discount is null || discount.IsDeleted)
        {
            return Result.Failure<CouponDetailDto>(Error.NotFound($"Discount '{request.DiscountId}' was not found."));
        }

        var existing = await repository.GetCouponByCodeAsync(Coupon.NormalizeCode(request.Code), cancellationToken)
            .ConfigureAwait(false);
        if (existing is not null && !existing.IsDeleted)
        {
            return Result.Failure<CouponDetailDto>(Error.Conflict("Coupon code already exists."));
        }

        var coupon = Coupon.Create(
            request.DiscountId,
            request.Code,
            request.IsActive,
            request.StartsAtUtc,
            request.EndsAtUtc,
            request.StoreId,
            request.GlobalUsageLimit,
            request.PerCustomerUsageLimit);

        await repository.AddCouponAsync(coupon, cancellationToken).ConfigureAwait(false);
        return Result.Success(MapDetail(coupon, discount.Name));
    }

    public async Task<Result<CouponDetailDto>> UpdateAsync(int id, UpdateCouponRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var coupon = await repository.GetCouponByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (coupon is null || coupon.IsDeleted)
        {
            return Result.Failure<CouponDetailDto>(Error.NotFound($"Coupon '{id}' was not found."));
        }

        coupon.Update(
            request.IsActive,
            request.StartsAtUtc,
            request.EndsAtUtc,
            request.StoreId,
            request.GlobalUsageLimit,
            request.PerCustomerUsageLimit);

        await repository.SaveCouponAsync(coupon, cancellationToken).ConfigureAwait(false);

        var discount = await repository.GetDiscountByIdAsync(coupon.DiscountId, cancellationToken).ConfigureAwait(false);
        return Result.Success(MapDetail(coupon, discount?.Name ?? "Unknown"));
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var coupon = await repository.GetCouponByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (coupon is null || coupon.IsDeleted)
        {
            return Result.Failure(Error.NotFound($"Coupon '{id}' was not found."));
        }

        coupon.SoftDelete();
        await repository.SaveCouponAsync(coupon, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    private static CouponSummaryDto MapSummary(Coupon c, string discountName) =>
        new(c.Id, c.Code, c.DiscountId, discountName, c.IsActive, c.UsageCount, c.GlobalUsageLimit,
            c.PerCustomerUsageLimit, c.StartsAtUtc, c.EndsAtUtc, c.StoreId);

    private static CouponDetailDto MapDetail(Coupon c, string discountName) =>
        new(c.Id, c.Code, c.DiscountId, discountName, c.IsActive, c.UsageCount, c.GlobalUsageLimit,
            c.PerCustomerUsageLimit, c.StartsAtUtc, c.EndsAtUtc, c.StoreId, c.CreatedAtUtc, c.UpdatedAtUtc);
}
