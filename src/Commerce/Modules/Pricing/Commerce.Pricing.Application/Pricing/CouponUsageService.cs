using Commerce.Pricing.Application.Abstractions;
using Commerce.Pricing.Contracts.Pricing;
using Commerce.Pricing.Domain.Entities;

namespace Commerce.Pricing.Application.Pricing;

public sealed class CouponUsageService(IPricingRepository repository) : ICouponUsageService
{
    public async Task<CouponUsageResult> TryConsumeAsync(
        CouponUsageRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var normalized = Coupon.NormalizeCode(request.Code);
        var coupon = await repository.GetCouponByCodeAsync(normalized, cancellationToken).ConfigureAwait(false);
        if (coupon is null)
        {
            return new CouponUsageResult(false, "Coupon not found.");
        }

        var consumed = await repository.TryConsumeCouponUsageAsync(
            coupon.Id,
            request.OrderId,
            request.CustomerId,
            coupon.GlobalUsageLimit,
            coupon.PerCustomerUsageLimit,
            cancellationToken).ConfigureAwait(false);

        return consumed
            ? new CouponUsageResult(true, null, coupon.Id)
            : new CouponUsageResult(false, "Coupon usage limit reached.");
    }
}
