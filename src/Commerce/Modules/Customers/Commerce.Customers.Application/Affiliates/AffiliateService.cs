using Commerce.Customers.Application.Abstractions;
using Commerce.Customers.Contracts.Affiliates;
using Commerce.Customers.Domain.Entities;
using Commerce.Customers.Domain.Enums;
using Commerce.Framework.Core.Errors;
using Commerce.Framework.Core.Results;

namespace Commerce.Customers.Application.Affiliates;

internal static class AffiliateMapper
{
    internal static AffiliateSummaryDto MapSummary(Affiliate affiliate) =>
        new(
            affiliate.Id,
            affiliate.CustomerId,
            affiliate.StoreId,
            affiliate.ReferralCode,
            affiliate.CommissionRatePercent,
            affiliate.IsActive,
            affiliate.CreatedAtUtc);

    internal static AffiliateDetailDto MapDetail(Affiliate affiliate, AffiliateCommissionAccount? account) =>
        new(
            affiliate.Id,
            affiliate.CustomerId,
            affiliate.StoreId,
            affiliate.ReferralCode,
            affiliate.CommissionRatePercent,
            affiliate.IsActive,
            account?.Balance ?? 0m,
            account?.CurrencyCode ?? "USD",
            affiliate.CreatedAtUtc,
            affiliate.UpdatedAtUtc);

    internal static AffiliateCommissionTransactionDto MapTransaction(AffiliateCommissionTransaction transaction) =>
        new(
            transaction.Id,
            transaction.Type,
            transaction.AmountDelta,
            transaction.BalanceAfter,
            transaction.CurrencyCode,
            transaction.Reason,
            transaction.CreatedAtUtc);

    internal static AffiliateReferralDto MapReferral(AffiliateReferral referral) =>
        new(
            referral.Id,
            referral.AffiliateId,
            referral.ReferredCustomerId,
            referral.StoreId,
            referral.ReferredAtUtc);
}

public sealed class AffiliateAdminService(IAffiliateRepository repository) : IAffiliateAdminService, IAffiliateReader
{
    public async Task<Result<IReadOnlyList<AffiliateSummaryDto>>> ListAsync(
        int? storeId,
        CancellationToken cancellationToken = default)
    {
        var affiliates = await repository.ListAsync(storeId, cancellationToken).ConfigureAwait(false);
        return Result.Success<IReadOnlyList<AffiliateSummaryDto>>(
            affiliates.Select(AffiliateMapper.MapSummary).ToList());
    }

    public async Task<Result<AffiliateDetailDto>> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        var affiliate = await repository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (affiliate is null)
        {
            return Result.Failure<AffiliateDetailDto>(Error.NotFound("Affiliate not found."));
        }

        var account = await repository
            .GetCommissionAccountWithTransactionsAsync(affiliate.Id, "USD", cancellationToken)
            .ConfigureAwait(false);

        return Result.Success(AffiliateMapper.MapDetail(affiliate, account));
    }

    public async Task<Result<AffiliateDetailDto>> CreateAsync(
        CreateAffiliateRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var normalized = Affiliate.NormalizeReferralCode(request.ReferralCode);
        var existingCode = await repository
            .GetByReferralCodeAsync(normalized, request.StoreId, cancellationToken)
            .ConfigureAwait(false);

        if (existingCode is not null)
        {
            return Result.Failure<AffiliateDetailDto>(Error.Conflict("Referral code already exists."));
        }

        var existingCustomer = await repository
            .GetByCustomerIdAsync(request.CustomerId, request.StoreId, cancellationToken)
            .ConfigureAwait(false);

        if (existingCustomer is not null)
        {
            return Result.Failure<AffiliateDetailDto>(Error.Conflict("Customer is already an affiliate for this store."));
        }

        try
        {
            var affiliate = Affiliate.Create(
                request.CustomerId,
                request.StoreId,
                request.ReferralCode,
                request.CommissionRatePercent,
                request.IsActive);

            await repository.AddAsync(affiliate, cancellationToken).ConfigureAwait(false);
            return Result.Success(AffiliateMapper.MapDetail(affiliate, null));
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return Result.Failure<AffiliateDetailDto>(Error.Validation(ex.Message));
        }
    }

    public async Task<Result<AffiliateDetailDto>> UpdateAsync(
        int id,
        UpdateAffiliateRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var affiliate = await repository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (affiliate is null)
        {
            return Result.Failure<AffiliateDetailDto>(Error.NotFound("Affiliate not found."));
        }

        try
        {
            affiliate.Update(request.CommissionRatePercent, request.IsActive);
            await repository.UpdateAsync(affiliate, cancellationToken).ConfigureAwait(false);

            var account = await repository
                .GetCommissionAccountWithTransactionsAsync(affiliate.Id, "USD", cancellationToken)
                .ConfigureAwait(false);

            return Result.Success(AffiliateMapper.MapDetail(affiliate, account));
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return Result.Failure<AffiliateDetailDto>(Error.Validation(ex.Message));
        }
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var affiliate = await repository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (affiliate is null)
        {
            return Result.Failure(Error.NotFound("Affiliate not found."));
        }

        affiliate.SoftDelete();
        await repository.UpdateAsync(affiliate, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    public async Task<Result<IReadOnlyList<AffiliateCommissionTransactionDto>>> ListCommissionsAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var affiliate = await repository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (affiliate is null)
        {
            return Result.Failure<IReadOnlyList<AffiliateCommissionTransactionDto>>(Error.NotFound("Affiliate not found."));
        }

        var account = await repository
            .GetCommissionAccountWithTransactionsAsync(affiliate.Id, "USD", cancellationToken)
            .ConfigureAwait(false);

        var transactions = (account?.Transactions ?? [])
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(AffiliateMapper.MapTransaction)
            .ToList();

        return Result.Success<IReadOnlyList<AffiliateCommissionTransactionDto>>(transactions);
    }

    public async Task<Result<IReadOnlyList<AffiliateReferralDto>>> ListReferralsAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var affiliate = await repository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (affiliate is null)
        {
            return Result.Failure<IReadOnlyList<AffiliateReferralDto>>(Error.NotFound("Affiliate not found."));
        }

        var referrals = await repository.ListReferralsAsync(id, cancellationToken).ConfigureAwait(false);
        return Result.Success<IReadOnlyList<AffiliateReferralDto>>(
            referrals.Select(AffiliateMapper.MapReferral).ToList());
    }
}

public sealed class AffiliateValidationService(IAffiliateRepository repository) : IAffiliateValidationService
{
    public async Task<AffiliateValidationResult> ValidateReferralCodeAsync(
        string referralCode,
        int storeId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(referralCode))
        {
            return new AffiliateValidationResult(false, null, null, ["Referral code is required."]);
        }

        var normalized = Affiliate.NormalizeReferralCode(referralCode);
        var affiliate = await repository.GetByReferralCodeAsync(normalized, storeId, cancellationToken).ConfigureAwait(false);

        if (affiliate is null || !affiliate.IsCurrentlyActive())
        {
            return new AffiliateValidationResult(false, null, null, ["Referral code is invalid or inactive."]);
        }

        return new AffiliateValidationResult(true, affiliate.Id, normalized, []);
    }
}

public sealed class AffiliateReferralService(IAffiliateRepository repository) : IAffiliateReferralService
{
    public async Task<Result<AffiliateReferralDto>> RecordReferralAsync(
        int affiliateId,
        int referredCustomerId,
        int storeId,
        CancellationToken cancellationToken = default)
    {
        var existing = await repository
            .GetReferralAsync(affiliateId, referredCustomerId, cancellationToken)
            .ConfigureAwait(false);

        if (existing is not null)
        {
            return Result.Success(AffiliateMapper.MapReferral(existing));
        }

        var referral = AffiliateReferral.Create(affiliateId, referredCustomerId, storeId);
        await repository.AddReferralAsync(referral, cancellationToken).ConfigureAwait(false);
        return Result.Success(AffiliateMapper.MapReferral(referral));
    }
}

public sealed class AffiliateCommissionService(IAffiliateRepository repository) : IAffiliateCommissionService
{
    public async Task<Result<AffiliateCommissionTransactionDto>> EarnCommissionAsync(
        int affiliateId,
        int storeId,
        string currencyCode,
        decimal orderTotal,
        decimal commissionRatePercent,
        int orderId,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        if (orderTotal <= 0m || commissionRatePercent <= 0m)
        {
            return Result.Failure<AffiliateCommissionTransactionDto>(Error.Validation("Order total and commission rate must be positive."));
        }

        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return Result.Failure<AffiliateCommissionTransactionDto>(Error.Validation("Idempotency key is required."));
        }

        var account = await repository
            .GetCommissionAccountWithTransactionsAsync(affiliateId, currencyCode, cancellationToken)
            .ConfigureAwait(false);

        if (account is null)
        {
            account = AffiliateCommissionAccount.Create(affiliateId, storeId, currencyCode);
            await repository.AddCommissionAccountAsync(account, cancellationToken).ConfigureAwait(false);
        }

        var commissionAmount = Math.Round(orderTotal * commissionRatePercent / 100m, 4, MidpointRounding.AwayFromZero);
        if (commissionAmount <= 0m)
        {
            return Result.Failure<AffiliateCommissionTransactionDto>(Error.Validation("Commission amount is zero."));
        }

        try
        {
            var transaction = account.PostTransaction(
                AffiliateCommissionTransactionType.Earn,
                commissionAmount,
                idempotencyKey.Trim(),
                AffiliateCommissionReferenceType.Order,
                orderId,
                $"Commission for order {orderId}.");

            await repository.UpdateCommissionAccountAsync(account, cancellationToken).ConfigureAwait(false);
            return Result.Success(AffiliateMapper.MapTransaction(transaction));
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return Result.Failure<AffiliateCommissionTransactionDto>(Error.Validation(ex.Message));
        }
    }
}
