using Commerce.Customers.Application.Abstractions;
using Commerce.Customers.Contracts.CustomerAccount;
using Commerce.Customers.Domain.Entities;
using Commerce.Customers.Domain.Enums;
using Commerce.Framework.Core.Errors;
using Commerce.Framework.Core.Results;

namespace Commerce.Customers.Application.CustomerAccount;

public sealed class LoyaltyService(
    ILoyaltyRepository repository,
    ICustomerActivityService activityService) : ILoyaltyService
{
    public async Task<Result<LoyaltyAccountDto>> GetAccountAsync(
        int customerId,
        int storeId,
        CancellationToken cancellationToken = default)
    {
        var account = await GetOrCreateAccountAsync(customerId, storeId, cancellationToken).ConfigureAwait(false);
        return Result.Success(CustomerAccountMapper.MapLoyaltyAccount(account));
    }

    public async Task<Result<IReadOnlyList<LoyaltyTransactionDto>>> ListTransactionsAsync(
        int customerId,
        int storeId,
        CancellationToken cancellationToken = default)
    {
        var account = await repository
            .GetAccountWithTransactionsAsync(customerId, storeId, cancellationToken)
            .ConfigureAwait(false);

        if (account is null)
        {
            return Result.Success<IReadOnlyList<LoyaltyTransactionDto>>([]);
        }

        return Result.Success<IReadOnlyList<LoyaltyTransactionDto>>(
            account.Transactions
                .OrderByDescending(x => x.CreatedAtUtc)
                .Select(CustomerAccountMapper.MapLoyaltyTransaction)
                .ToList());
    }

    public async Task<Result<LoyaltyTransactionDto>> EarnAsync(
        int customerId,
        int storeId,
        int points,
        string idempotencyKey,
        CustomerAccountReferenceType referenceType,
        int? referenceId,
        string? reason,
        DateTime? expiresAtUtc,
        CancellationToken cancellationToken = default)
    {
        if (points <= 0)
        {
            return Result.Failure<LoyaltyTransactionDto>(Error.Validation("Earn points must be greater than zero."));
        }

        try
        {
            var account = await GetOrCreateAccountAsync(customerId, storeId, cancellationToken).ConfigureAwait(false);
            var transaction = account.PostTransaction(
                LoyaltyTransactionType.Earn,
                points,
                idempotencyKey,
                referenceType,
                referenceId,
                reason,
                expiresAtUtc);

            await repository.UpdateAccountAsync(account, cancellationToken).ConfigureAwait(false);
            await activityService.LogAsync(
                customerId,
                storeId,
                CustomerActivityType.PointsEarned,
                $"Earned {points} loyalty points.",
                cancellationToken: cancellationToken).ConfigureAwait(false);

            return Result.Success(CustomerAccountMapper.MapLoyaltyTransaction(transaction));
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return Result.Failure<LoyaltyTransactionDto>(Error.Validation(ex.Message));
        }
    }

    public async Task<Result<LoyaltyTransactionDto>> SpendAsync(
        int customerId,
        int storeId,
        int points,
        string idempotencyKey,
        CustomerAccountReferenceType referenceType,
        int? referenceId,
        string? reason,
        CancellationToken cancellationToken = default)
    {
        if (points <= 0)
        {
            return Result.Failure<LoyaltyTransactionDto>(Error.Validation("Spend points must be greater than zero."));
        }

        try
        {
            var account = await GetOrCreateAccountAsync(customerId, storeId, cancellationToken).ConfigureAwait(false);
            var transaction = account.PostTransaction(
                LoyaltyTransactionType.Spend,
                -points,
                idempotencyKey,
                referenceType,
                referenceId,
                reason);

            await repository.UpdateAccountAsync(account, cancellationToken).ConfigureAwait(false);
            await activityService.LogAsync(
                customerId,
                storeId,
                CustomerActivityType.PointsSpent,
                $"Spent {points} loyalty points.",
                cancellationToken: cancellationToken).ConfigureAwait(false);

            return Result.Success(CustomerAccountMapper.MapLoyaltyTransaction(transaction));
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return Result.Failure<LoyaltyTransactionDto>(Error.Validation(ex.Message));
        }
    }

    public async Task<Result<int>> ExpirePointsAsync(
        int customerId,
        int storeId,
        CancellationToken cancellationToken = default)
    {
        var account = await repository
            .GetAccountWithTransactionsAsync(customerId, storeId, cancellationToken)
            .ConfigureAwait(false);

        if (account is null)
        {
            return Result.Success(0);
        }

        var utcNow = DateTime.UtcNow;
        var expiredTotal = 0;
        foreach (var earnTransaction in account.Transactions
                     .Where(x =>
                         x.Type == LoyaltyTransactionType.Earn &&
                         x.ExpiresAtUtc.HasValue &&
                         x.ExpiresAtUtc.Value <= utcNow &&
                         !x.IsExpired)
                     .ToList())
        {
            var points = earnTransaction.PointsDelta;
            if (points <= 0)
            {
                continue;
            }

            try
            {
                account.PostTransaction(
                    LoyaltyTransactionType.Expire,
                    -points,
                    $"expire-{earnTransaction.Id}",
                    CustomerAccountReferenceType.None,
                    earnTransaction.Id,
                    "Points expired.");

                earnTransaction.MarkExpired();
                expiredTotal += points;
            }
            catch (InvalidOperationException)
            {
                // Balance may already be reduced; skip.
            }
        }

        if (expiredTotal > 0)
        {
            await repository.UpdateAccountAsync(account, cancellationToken).ConfigureAwait(false);
        }

        return Result.Success(expiredTotal);
    }

    public async Task<Result<LoyaltyRewardRedemptionDto>> RedeemRewardAsync(
        int customerId,
        int storeId,
        RedeemLoyaltyRewardRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return Result.Failure<LoyaltyRewardRedemptionDto>(Error.Validation("Idempotency key is required."));
        }

        var existingRedemption = await repository
            .GetRedemptionByIdempotencyKeyAsync(customerId, storeId, idempotencyKey.Trim(), cancellationToken)
            .ConfigureAwait(false);

        if (existingRedemption is not null)
        {
            return Result.Success(new LoyaltyRewardRedemptionDto(
                existingRedemption.Id,
                existingRedemption.LoyaltyRewardId,
                existingRedemption.PointsSpent,
                existingRedemption.Status,
                existingRedemption.CreatedAtUtc));
        }

        var reward = await repository.GetRewardByIdAsync(request.RewardId, cancellationToken).ConfigureAwait(false);
        if (reward is null || !reward.IsActive || reward.StoreId != storeId)
        {
            return Result.Failure<LoyaltyRewardRedemptionDto>(Error.NotFound($"Reward '{request.RewardId}' was not found."));
        }

        var spendResult = await SpendAsync(
            customerId,
            storeId,
            reward.PointsCost,
            $"{idempotencyKey}-spend",
            CustomerAccountReferenceType.Reward,
            reward.Id,
            $"Redeemed reward '{reward.Name}'.",
            cancellationToken).ConfigureAwait(false);

        if (spendResult.IsFailure)
        {
            return Result.Failure<LoyaltyRewardRedemptionDto>(spendResult.Error!);
        }

        var redemption = LoyaltyRewardRedemption.Create(
            customerId,
            storeId,
            reward.Id,
            reward.PointsCost,
            idempotencyKey.Trim());

        redemption.Complete(spendResult.Value!.Id);
        await repository.AddRedemptionAsync(redemption, cancellationToken).ConfigureAwait(false);

        await activityService.LogAsync(
            customerId,
            storeId,
            CustomerActivityType.RewardRedeemed,
            $"Redeemed reward '{reward.Name}'.",
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return Result.Success(new LoyaltyRewardRedemptionDto(
            redemption.Id,
            redemption.LoyaltyRewardId,
            redemption.PointsSpent,
            redemption.Status,
            redemption.CreatedAtUtc));
    }

    private async Task<LoyaltyAccount> GetOrCreateAccountAsync(
        int customerId,
        int storeId,
        CancellationToken cancellationToken)
    {
        var account = await repository
            .GetAccountWithTransactionsAsync(customerId, storeId, cancellationToken)
            .ConfigureAwait(false);

        if (account is not null)
        {
            return account;
        }

        account = LoyaltyAccount.Create(customerId, storeId);
        await repository.AddAccountAsync(account, cancellationToken).ConfigureAwait(false);
        return account;
    }
}

public sealed class LoyaltyRewardAdminService(ILoyaltyRepository repository) : ILoyaltyRewardAdminService
{
    public async Task<Result<IReadOnlyList<LoyaltyRewardDto>>> ListAsync(
        int? storeId,
        CancellationToken cancellationToken = default)
    {
        var rewards = await repository.ListRewardsAsync(storeId, cancellationToken).ConfigureAwait(false);
        return Result.Success<IReadOnlyList<LoyaltyRewardDto>>(
            rewards.Select(CustomerAccountMapper.MapReward).ToList());
    }

    public async Task<Result<LoyaltyRewardDto>> CreateAsync(
        CreateLoyaltyRewardRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var reward = LoyaltyReward.Create(request.StoreId, request.Name, request.PointsCost, request.Description);
            await repository.AddRewardAsync(reward, cancellationToken).ConfigureAwait(false);
            return Result.Success(CustomerAccountMapper.MapReward(reward));
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return Result.Failure<LoyaltyRewardDto>(Error.Validation(ex.Message));
        }
    }

    public async Task<Result<LoyaltyRewardDto>> UpdateAsync(
        int id,
        UpdateLoyaltyRewardRequest request,
        CancellationToken cancellationToken = default)
    {
        var reward = await repository.GetRewardByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (reward is null)
        {
            return Result.Failure<LoyaltyRewardDto>(Error.NotFound($"Reward '{id}' was not found."));
        }

        try
        {
            reward.Update(request.Name, request.PointsCost, request.Description, request.IsActive);
            await repository.UpdateRewardAsync(reward, cancellationToken).ConfigureAwait(false);
            return Result.Success(CustomerAccountMapper.MapReward(reward));
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return Result.Failure<LoyaltyRewardDto>(Error.Validation(ex.Message));
        }
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var reward = await repository.GetRewardByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (reward is null)
        {
            return Result.Failure(Error.NotFound($"Reward '{id}' was not found."));
        }

        await repository.DeleteRewardAsync(reward, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}
