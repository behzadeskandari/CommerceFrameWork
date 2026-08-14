using Commerce.Pricing.Application.Abstractions;
using Commerce.Pricing.Contracts.Discounts;
using Commerce.Pricing.Domain.Entities;
using Commerce.Pricing.Domain.Enums;
using Commerce.Framework.Core.Errors;
using Commerce.Framework.Core.Results;

namespace Commerce.Pricing.Application.Discounts;

public sealed class DiscountAdminService(IPricingRepository repository) : IDiscountAdminService
{
    public async Task<Result<IReadOnlyList<DiscountSummaryDto>>> ListAsync(CancellationToken cancellationToken = default)
    {
        var discounts = await repository.ListDiscountsAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success<IReadOnlyList<DiscountSummaryDto>>(
            discounts.Where(d => !d.IsDeleted).Select(MapSummary).ToList());
    }

    public async Task<Result<DiscountDetailDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var discount = await repository.GetDiscountByIdAsync(id, cancellationToken).ConfigureAwait(false);
        return discount is null || discount.IsDeleted
            ? Result.Failure<DiscountDetailDto>(Error.NotFound($"Discount '{id}' was not found."))
            : Result.Success(MapDetail(discount));
    }

    public async Task<Result<DiscountDetailDto>> CreateAsync(CreateDiscountRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var targets = MapTargets(request.Targets);
        var discount = Discount.Create(
            request.Name,
            request.SystemName,
            request.Description,
            request.DiscountType,
            request.Value,
            request.CurrencyCode,
            request.Priority,
            request.IsActive,
            request.StartsAtUtc,
            request.EndsAtUtc,
            request.StoreId,
            request.StackingMode,
            request.MaximumDiscountAmount,
            request.MinimumCartSubtotal,
            request.MinimumQuantity,
            request.CustomerEligibility,
            request.SpecificCustomerId,
            request.CustomerGroupId,
            request.ApplicationScope,
            targets);

        await repository.AddDiscountAsync(discount, cancellationToken).ConfigureAwait(false);
        return Result.Success(MapDetail(discount));
    }

    public async Task<Result<DiscountDetailDto>> UpdateAsync(int id, UpdateDiscountRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var discount = await repository.GetDiscountByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (discount is null || discount.IsDeleted)
        {
            return Result.Failure<DiscountDetailDto>(Error.NotFound($"Discount '{id}' was not found."));
        }

        discount.Update(
            request.Name,
            request.Description,
            request.DiscountType,
            request.Value,
            request.CurrencyCode,
            request.Priority,
            request.StartsAtUtc,
            request.EndsAtUtc,
            request.StoreId,
            request.StackingMode,
            request.MaximumDiscountAmount,
            request.MinimumCartSubtotal,
            request.MinimumQuantity,
            request.CustomerEligibility,
            request.SpecificCustomerId,
            request.CustomerGroupId,
            request.ApplicationScope,
            MapTargets(request.Targets));

        await repository.SaveDiscountAsync(discount, cancellationToken).ConfigureAwait(false);
        return Result.Success(MapDetail(discount));
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var discount = await repository.GetDiscountByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (discount is null || discount.IsDeleted)
        {
            return Result.Failure(Error.NotFound($"Discount '{id}' was not found."));
        }

        discount.SoftDelete();
        await repository.SaveDiscountAsync(discount, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    public async Task<Result> ActivateAsync(int id, CancellationToken cancellationToken = default)
    {
        var discount = await repository.GetDiscountByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (discount is null || discount.IsDeleted)
        {
            return Result.Failure(Error.NotFound($"Discount '{id}' was not found."));
        }

        discount.Activate();
        await repository.SaveDiscountAsync(discount, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    public async Task<Result> DeactivateAsync(int id, CancellationToken cancellationToken = default)
    {
        var discount = await repository.GetDiscountByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (discount is null || discount.IsDeleted)
        {
            return Result.Failure(Error.NotFound($"Discount '{id}' was not found."));
        }

        discount.Deactivate();
        await repository.SaveDiscountAsync(discount, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    private static IEnumerable<DiscountTarget> MapTargets(IReadOnlyList<DiscountTargetDto> targets) =>
        targets.Select(t => DiscountTarget.Create(0, t.TargetType, t.TargetId));

    private static DiscountSummaryDto MapSummary(Discount d) =>
        new(d.Id, d.Name, d.SystemName, d.DiscountType, d.Value, d.CurrencyCode, d.Priority, d.IsActive,
            d.StartsAtUtc, d.EndsAtUtc, d.StoreId, d.ApplicationScope);

    private static DiscountDetailDto MapDetail(Discount d) =>
        new(d.Id, d.Name, d.SystemName, d.Description, d.DiscountType, d.Value, d.CurrencyCode, d.Priority,
            d.IsActive, d.StartsAtUtc, d.EndsAtUtc, d.StoreId, d.StackingMode, d.MaximumDiscountAmount,
            d.MinimumCartSubtotal, d.MinimumQuantity, d.CustomerEligibility, d.SpecificCustomerId, d.CustomerGroupId,
            d.ApplicationScope,
            d.Targets.Select(t => new DiscountTargetDto(t.TargetType, t.TargetId)).ToList(),
            d.CreatedAtUtc, d.UpdatedAtUtc);
}
