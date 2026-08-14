using Commerce.Framework.Core.Errors;
using Commerce.Framework.Core.Results;
using Commerce.Promotions.Application.Abstractions;
using Commerce.Promotions.Contracts.Admin;
using Commerce.Promotions.Domain.Entities;
using Commerce.Promotions.Domain.Enums;

namespace Commerce.Promotions.Application.Admin;

public sealed class PromotionAdminService(IPromotionsRepository repository) : IPromotionAdminService
{
    public async Task<Result<IReadOnlyList<PromotionSummaryDto>>> ListAsync(int? storeId, CancellationToken cancellationToken = default)
    {
        var items = await repository.ListPromotionsAsync(storeId, cancellationToken).ConfigureAwait(false);
        return Result.Success<IReadOnlyList<PromotionSummaryDto>>(items.Select(MapSummary).ToList());
    }

    public async Task<Result<PromotionDetailDto>> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        var promotion = await repository.GetPromotionByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (promotion is null)
        {
            return Result.Failure<PromotionDetailDto>(Error.NotFound("Promotion not found."));
        }

        return Result.Success(MapDetail(promotion));
    }

    public async Task<Result<PromotionDetailDto>> CreateAsync(CreatePromotionRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var promotion = Promotion.Create(
                request.Name,
                request.SystemName,
                request.Description,
                request.IsActive,
                request.StartsAtUtc,
                request.EndsAtUtc,
                request.StoreId,
                request.Priority,
                request.CombinationRule,
                request.CombinationGroup,
                request.GlobalUsageLimit,
                request.PerCustomerUsageLimit,
                request.RequiresCouponCode,
                request.CouponCode,
                MapConditions(request.Conditions),
                MapActions(request.Actions));

            await repository.AddPromotionAsync(promotion, cancellationToken).ConfigureAwait(false);
            await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Result.Success(MapDetail(promotion));
        }
        catch (ArgumentException ex)
        {
            return Result.Failure<PromotionDetailDto>(Error.Validation(ex.Message));
        }
    }

    public async Task<Result<PromotionDetailDto>> UpdateAsync(int id, UpdatePromotionRequest request, CancellationToken cancellationToken = default)
    {
        var promotion = await repository.GetPromotionByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (promotion is null)
        {
            return Result.Failure<PromotionDetailDto>(Error.NotFound("Promotion not found."));
        }

        try
        {
            promotion.Update(
                request.Name,
                request.Description,
                request.IsActive,
                request.StartsAtUtc,
                request.EndsAtUtc,
                request.StoreId,
                request.Priority,
                request.CombinationRule,
                request.CombinationGroup,
                request.GlobalUsageLimit,
                request.PerCustomerUsageLimit,
                request.RequiresCouponCode,
                request.CouponCode,
                MapConditions(request.Conditions),
                MapActions(request.Actions));

            await repository.SavePromotionAsync(promotion, cancellationToken).ConfigureAwait(false);
            await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Result.Success(MapDetail(promotion));
        }
        catch (ArgumentException ex)
        {
            return Result.Failure<PromotionDetailDto>(Error.Validation(ex.Message));
        }
    }

    public async Task<Result> ActivateAsync(int id, CancellationToken cancellationToken = default)
    {
        var promotion = await repository.GetPromotionByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (promotion is null)
        {
            return Result.Failure(Error.NotFound("Promotion not found."));
        }

        promotion.Activate();
        await repository.SavePromotionAsync(promotion, cancellationToken).ConfigureAwait(false);
        await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    public async Task<Result> DeactivateAsync(int id, CancellationToken cancellationToken = default)
    {
        var promotion = await repository.GetPromotionByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (promotion is null)
        {
            return Result.Failure(Error.NotFound("Promotion not found."));
        }

        promotion.Deactivate();
        await repository.SavePromotionAsync(promotion, cancellationToken).ConfigureAwait(false);
        await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var promotion = await repository.GetPromotionByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (promotion is null)
        {
            return Result.Failure(Error.NotFound("Promotion not found."));
        }

        promotion.SoftDelete();
        await repository.SavePromotionAsync(promotion, cancellationToken).ConfigureAwait(false);
        await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    private static IEnumerable<PromotionCondition> MapConditions(IReadOnlyList<PromotionConditionRequest> conditions) =>
        conditions.Select(x => PromotionCondition.Create(0, x.ConditionType, x.ParametersJson));

    private static IEnumerable<PromotionAction> MapActions(IReadOnlyList<PromotionActionRequest> actions) =>
        actions.Select(x => PromotionAction.Create(0, x.ActionType, x.TargetScope, x.ParametersJson));

    private static PromotionSummaryDto MapSummary(Promotion promotion) =>
        new(
            promotion.Id,
            promotion.Name,
            promotion.SystemName,
            promotion.IsActive,
            promotion.StartsAtUtc,
            promotion.EndsAtUtc,
            promotion.StoreId,
            promotion.Priority,
            promotion.CombinationRule,
            promotion.UsageCount,
            promotion.GlobalUsageLimit);

    private static PromotionDetailDto MapDetail(Promotion promotion) =>
        new(
            promotion.Id,
            promotion.Name,
            promotion.SystemName,
            promotion.Description,
            promotion.IsActive,
            promotion.StartsAtUtc,
            promotion.EndsAtUtc,
            promotion.StoreId,
            promotion.Priority,
            promotion.CombinationRule,
            promotion.CombinationGroup,
            promotion.GlobalUsageLimit,
            promotion.PerCustomerUsageLimit,
            promotion.UsageCount,
            promotion.RequiresCouponCode,
            promotion.CouponCode,
            promotion.Conditions.Select(x => new PromotionConditionDto(x.Id, x.ConditionType, x.ParametersJson)).ToList(),
            promotion.Actions.Select(x => new PromotionActionDto(x.Id, x.ActionType, x.TargetScope, x.ParametersJson)).ToList(),
            promotion.CreatedAtUtc,
            promotion.UpdatedAtUtc);
}
