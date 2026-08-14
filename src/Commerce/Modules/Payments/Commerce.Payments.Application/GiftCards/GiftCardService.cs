using Commerce.Framework.Core.Errors;
using Commerce.Framework.Core.Results;
using Commerce.Payments.Application.Abstractions;
using Commerce.Payments.Contracts.GiftCards;
using Commerce.Payments.Domain.Entities;
using Commerce.Payments.Domain.Enums;

namespace Commerce.Payments.Application.GiftCards;

internal static class GiftCardMapper
{
    internal static GiftCardSummaryDto MapSummary(GiftCard card) =>
        new(
            card.Id,
            card.Code,
            card.StoreId,
            card.CurrencyCode,
            card.InitialAmount,
            card.Balance,
            card.IsActive,
            card.ExpiresAtUtc,
            card.CreatedAtUtc);

    internal static GiftCardDetailDto MapDetail(GiftCard card) =>
        new(
            card.Id,
            card.Code,
            card.StoreId,
            card.CurrencyCode,
            card.InitialAmount,
            card.Balance,
            card.IsActive,
            card.StartsAtUtc,
            card.ExpiresAtUtc,
            card.RecipientEmail,
            card.PurchasedByCustomerId,
            card.RecipientCustomerId,
            card.CreatedAtUtc,
            card.UpdatedAtUtc);

    internal static GiftCardTransactionDto MapTransaction(GiftCardTransaction transaction) =>
        new(
            transaction.Id,
            transaction.Type,
            transaction.AmountDelta,
            transaction.BalanceAfter,
            transaction.CurrencyCode,
            transaction.Reason,
            transaction.CreatedAtUtc);
}

public sealed class GiftCardAdminService(IGiftCardRepository repository) : IGiftCardAdminService
{
    public async Task<Result<IReadOnlyList<GiftCardSummaryDto>>> ListAsync(
        int? storeId,
        CancellationToken cancellationToken = default)
    {
        var cards = await repository.ListAsync(storeId, cancellationToken).ConfigureAwait(false);
        return Result.Success<IReadOnlyList<GiftCardSummaryDto>>(
            cards.Select(GiftCardMapper.MapSummary).ToList());
    }

    public async Task<Result<GiftCardDetailDto>> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        var card = await repository.GetByIdWithTransactionsAsync(id, cancellationToken).ConfigureAwait(false);
        return card is null
            ? Result.Failure<GiftCardDetailDto>(Error.NotFound("Gift card not found."))
            : Result.Success(GiftCardMapper.MapDetail(card));
    }

    public async Task<Result<GiftCardDetailDto>> CreateAsync(
        CreateGiftCardRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var existing = await repository
            .GetByCodeWithTransactionsAsync(GiftCard.NormalizeCode(request.Code), cancellationToken)
            .ConfigureAwait(false);

        if (existing is not null)
        {
            return Result.Failure<GiftCardDetailDto>(Error.Conflict("Gift card code already exists."));
        }

        try
        {
            var card = GiftCard.Create(
                request.Code,
                request.StoreId,
                request.CurrencyCode,
                request.InitialAmount,
                request.IsActive,
                request.StartsAtUtc,
                request.ExpiresAtUtc,
                request.RecipientEmail,
                request.PurchasedByCustomerId,
                request.RecipientCustomerId);

            await repository.AddAsync(card, cancellationToken).ConfigureAwait(false);
            return Result.Success(GiftCardMapper.MapDetail(card));
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return Result.Failure<GiftCardDetailDto>(Error.Validation(ex.Message));
        }
    }

    public async Task<Result<GiftCardDetailDto>> UpdateAsync(
        int id,
        UpdateGiftCardRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var card = await repository.GetByIdWithTransactionsAsync(id, cancellationToken).ConfigureAwait(false);
        if (card is null)
        {
            return Result.Failure<GiftCardDetailDto>(Error.NotFound("Gift card not found."));
        }

        try
        {
            card.Update(
                request.IsActive,
                request.StartsAtUtc,
                request.ExpiresAtUtc,
                request.RecipientEmail,
                request.RecipientCustomerId);

            await repository.UpdateAsync(card, cancellationToken).ConfigureAwait(false);
            return Result.Success(GiftCardMapper.MapDetail(card));
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return Result.Failure<GiftCardDetailDto>(Error.Validation(ex.Message));
        }
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var card = await repository.GetByIdWithTransactionsAsync(id, cancellationToken).ConfigureAwait(false);
        if (card is null)
        {
            return Result.Failure(Error.NotFound("Gift card not found."));
        }

        card.SoftDelete();
        await repository.UpdateAsync(card, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    public async Task<Result<IReadOnlyList<GiftCardTransactionDto>>> ListTransactionsAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var card = await repository.GetByIdWithTransactionsAsync(id, cancellationToken).ConfigureAwait(false);
        if (card is null)
        {
            return Result.Failure<IReadOnlyList<GiftCardTransactionDto>>(Error.NotFound("Gift card not found."));
        }

        var transactions = card.Transactions
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(GiftCardMapper.MapTransaction)
            .ToList();

        return Result.Success<IReadOnlyList<GiftCardTransactionDto>>(transactions);
    }
}

public sealed class GiftCardValidationService(IGiftCardRepository repository) : IGiftCardValidationService
{
    public async Task<GiftCardValidationResult> ValidateAsync(
        GiftCardValidationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(request.Code))
        {
            return Invalid(["Gift card code is required."]);
        }

        var normalized = GiftCard.NormalizeCode(request.Code);
        var card = await repository.GetByCodeWithTransactionsAsync(normalized, cancellationToken).ConfigureAwait(false);

        if (card is null)
        {
            return Invalid(["Gift card not found."]);
        }

        if (!card.IsCurrentlyValid(request.CurrentTimeUtc))
        {
            errors.Add("Gift card is expired or inactive.");
        }

        if (!card.AppliesToStore(request.StoreId))
        {
            errors.Add("Gift card is not valid for this store.");
        }

        if (!string.Equals(card.CurrencyCode, request.CurrencyCode, StringComparison.OrdinalIgnoreCase))
        {
            errors.Add("Gift card currency does not match cart currency.");
        }

        if (card.Balance <= 0m)
        {
            errors.Add("Gift card has no remaining balance.");
        }

        if (request.RequestedAmount > 0m && card.Balance < request.RequestedAmount)
        {
            errors.Add("Gift card balance is insufficient for requested amount.");
        }

        return errors.Count > 0
            ? Invalid(errors)
            : new GiftCardValidationResult(true, normalized, card.Id, card.Balance, []);
    }

    private static GiftCardValidationResult Invalid(IReadOnlyList<string> errors) =>
        new(false, null, null, 0m, errors);
}

public sealed class GiftCardRedemptionService(IGiftCardRepository repository) : IGiftCardRedemptionService
{
    public async Task<GiftCardRedemptionResult> TryRedeemAsync(
        GiftCardRedemptionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Amount <= 0m)
        {
            return new GiftCardRedemptionResult(false, "Redemption amount must be greater than zero.");
        }

        var normalized = GiftCard.NormalizeCode(request.Code);
        var card = await repository.GetByCodeWithTransactionsAsync(normalized, cancellationToken).ConfigureAwait(false);
        if (card is null)
        {
            return new GiftCardRedemptionResult(false, "Gift card not found.");
        }

        var validation = await new GiftCardValidationService(repository)
            .ValidateAsync(
                new GiftCardValidationRequest(
                    normalized,
                    request.StoreId,
                    request.CurrencyCode,
                    request.Amount,
                    DateTime.UtcNow),
                cancellationToken)
            .ConfigureAwait(false);

        if (!validation.IsValid)
        {
            return new GiftCardRedemptionResult(false, string.Join(' ', validation.Errors));
        }

        var redeemed = await repository.TryRedeemAsync(
            card.Id,
            request.Amount,
            request.OrderId,
            request.IdempotencyKey,
            cancellationToken).ConfigureAwait(false);

        return redeemed
            ? new GiftCardRedemptionResult(true, null, card.Id, request.Amount)
            : new GiftCardRedemptionResult(false, "Gift card redemption failed due to concurrency or insufficient balance.");
    }
}

public sealed class GiftCardReader(IGiftCardRepository repository) : IGiftCardReader
{
    public async Task<Result<GiftCardDetailDto>> GetByCodeAsync(
        string code,
        int storeId,
        CancellationToken cancellationToken = default)
    {
        var card = await repository
            .GetByCodeWithTransactionsAsync(GiftCard.NormalizeCode(code), cancellationToken)
            .ConfigureAwait(false);

        if (card is null || !card.AppliesToStore(storeId))
        {
            return Result.Failure<GiftCardDetailDto>(Error.NotFound("Gift card not found."));
        }

        return Result.Success(GiftCardMapper.MapDetail(card));
    }
}
