using Commerce.Pricing.Application.Abstractions;
using Commerce.Pricing.Contracts.Pricing;
using Commerce.Pricing.Domain.Entities;

namespace Commerce.Pricing.Application.Pricing;

public sealed class CouponValidationService(IPricingRepository repository) : ICouponValidationService
{
    public async Task<CouponValidationResult> ValidateAsync(
        CouponValidationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(request.Code))
        {
            return Invalid(["Coupon code is required."]);
        }

        var normalized = Coupon.NormalizeCode(request.Code);
        var coupon = await repository.GetCouponByCodeAsync(normalized, cancellationToken).ConfigureAwait(false);

        if (coupon is null)
        {
            return Invalid(["Coupon not found."]);
        }

        if (!coupon.IsCurrentlyValid(request.CurrentTimeUtc))
        {
            errors.Add("Coupon is expired or inactive.");
        }

        if (!coupon.AppliesToStore(request.StoreId))
        {
            errors.Add("Coupon is not valid for this store.");
        }

        if (!coupon.HasGlobalUsageRemaining())
        {
            errors.Add("Coupon usage limit reached.");
        }

        if (request.CustomerId.HasValue)
        {
            var customerUsage = await repository
                .GetCustomerCouponUsageCountAsync(coupon.Id, request.CustomerId.Value, cancellationToken)
                .ConfigureAwait(false);

            if (coupon.PerCustomerUsageLimit.HasValue && customerUsage >= coupon.PerCustomerUsageLimit.Value)
            {
                errors.Add("Coupon usage limit reached for this customer.");
            }
        }
        else if (coupon.PerCustomerUsageLimit.HasValue)
        {
            errors.Add("Coupon requires an authenticated customer.");
        }

        var discount = await repository.GetDiscountByIdAsync(coupon.DiscountId, cancellationToken).ConfigureAwait(false);
        if (discount is null || discount.IsDeleted)
        {
            errors.Add("Coupon discount is no longer available.");
        }
        else
        {
            if (!discount.IsCurrentlyValid(request.CurrentTimeUtc))
            {
                errors.Add("Coupon discount is expired or inactive.");
            }

            if (!discount.AppliesToStore(request.StoreId))
            {
                errors.Add("Coupon is not valid for this store.");
            }

            if (!discount.IsEligibleForCustomer(request.CustomerId, request.IsGuest, request.CustomerGroupId))
            {
                errors.Add("Coupon is not available for this customer.");
            }

            if (discount.MinimumCartSubtotal.HasValue && request.CartSubtotal < discount.MinimumCartSubtotal.Value)
            {
                errors.Add($"Minimum cart subtotal of {discount.MinimumCartSubtotal.Value} not met.");
            }

            if (discount.DiscountType is Domain.Enums.DiscountType.FixedAmount &&
                !string.Equals(discount.CurrencyCode, request.CurrencyCode, StringComparison.OrdinalIgnoreCase))
            {
                errors.Add("Coupon currency does not match cart currency.");
            }
        }

        if (errors.Count > 0)
        {
            return Invalid(errors);
        }

        return new CouponValidationResult(true, normalized, coupon.Id, coupon.DiscountId, []);
    }

    private static CouponValidationResult Invalid(IReadOnlyList<string> errors) =>
        new(false, null, null, null, errors);
}
