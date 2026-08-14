using Commerce.Customers.Application.Abstractions;
using Commerce.Customers.Contracts.CustomerAccount;
using Commerce.Customers.Domain.Entities;
using Commerce.Customers.Domain.Enums;
using Commerce.Framework.Core.Errors;
using Commerce.Framework.Core.Results;

namespace Commerce.Customers.Application.CustomerAccount;

public sealed class StoreCreditService(
    IStoreCreditRepository repository,
    ICustomerActivityService activityService) : IStoreCreditService, IStoreCreditReader
{
    public Task<Result<StoreCreditAccountDto>> GetAvailableCreditAsync(
        int customerId,
        int storeId,
        string currencyCode,
        CancellationToken cancellationToken = default) =>
        GetAccountAsync(customerId, storeId, currencyCode, cancellationToken);

    public async Task<Result<StoreCreditAccountDto>> GetAccountAsync(
        int customerId,
        int storeId,
        string currencyCode,
        CancellationToken cancellationToken = default)
    {
        var account = await GetOrCreateAccountAsync(customerId, storeId, currencyCode, cancellationToken).ConfigureAwait(false);
        return Result.Success(CustomerAccountMapper.MapStoreCreditAccount(account));
    }

    public async Task<Result<IReadOnlyList<StoreCreditTransactionDto>>> ListTransactionsAsync(
        int customerId,
        int storeId,
        CancellationToken cancellationToken = default)
    {
        var accounts = await ListAccountsForCustomerStoreAsync(customerId, storeId, cancellationToken).ConfigureAwait(false);
        var transactions = accounts
            .SelectMany(x => x.Transactions)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(CustomerAccountMapper.MapStoreCreditTransaction)
            .ToList();

        return Result.Success<IReadOnlyList<StoreCreditTransactionDto>>(transactions);
    }

    public async Task<Result<StoreCreditTransactionDto>> CreditAsync(
        int customerId,
        int storeId,
        string currencyCode,
        CreditStoreCreditRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Amount <= 0m)
        {
            return Result.Failure<StoreCreditTransactionDto>(Error.Validation("Credit amount must be greater than zero."));
        }

        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return Result.Failure<StoreCreditTransactionDto>(Error.Validation("Idempotency key is required."));
        }

        try
        {
            var account = await GetOrCreateAccountAsync(customerId, storeId, currencyCode, cancellationToken).ConfigureAwait(false);
            var transaction = account.PostTransaction(
                StoreCreditTransactionType.Credit,
                request.Amount,
                idempotencyKey.Trim(),
                CustomerAccountReferenceType.Manual,
                null,
                request.Reason,
                request.ExpiresAtUtc);

            await repository.UpdateAccountAsync(account, cancellationToken).ConfigureAwait(false);
            return Result.Success(CustomerAccountMapper.MapStoreCreditTransaction(transaction));
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return Result.Failure<StoreCreditTransactionDto>(Error.Validation(ex.Message));
        }
    }

    public async Task<Result<StoreCreditTransactionDto>> DebitAsync(
        int customerId,
        int storeId,
        string currencyCode,
        ApplyStoreCreditRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Amount <= 0m)
        {
            return Result.Failure<StoreCreditTransactionDto>(Error.Validation("Debit amount must be greater than zero."));
        }

        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return Result.Failure<StoreCreditTransactionDto>(Error.Validation("Idempotency key is required."));
        }

        try
        {
            var account = await GetOrCreateAccountAsync(customerId, storeId, currencyCode, cancellationToken).ConfigureAwait(false);
            var transaction = account.PostTransaction(
                StoreCreditTransactionType.Debit,
                -request.Amount,
                idempotencyKey.Trim(),
                request.OrderId.HasValue ? CustomerAccountReferenceType.Order : CustomerAccountReferenceType.Manual,
                request.OrderId,
                request.Reason);

            await repository.UpdateAccountAsync(account, cancellationToken).ConfigureAwait(false);
            await activityService.LogAsync(
                customerId,
                storeId,
                CustomerActivityType.StoreCreditApplied,
                $"Applied {request.Amount} store credit.",
                cancellationToken: cancellationToken).ConfigureAwait(false);

            return Result.Success(CustomerAccountMapper.MapStoreCreditTransaction(transaction));
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return Result.Failure<StoreCreditTransactionDto>(Error.Validation(ex.Message));
        }
    }

    public async Task<Result<decimal>> ExpireCreditAsync(
        int customerId,
        int storeId,
        string currencyCode,
        CancellationToken cancellationToken = default)
    {
        var account = await repository
            .GetAccountWithTransactionsAsync(customerId, storeId, currencyCode, cancellationToken)
            .ConfigureAwait(false);

        if (account is null)
        {
            return Result.Success(0m);
        }

        var utcNow = DateTime.UtcNow;
        var expiredTotal = 0m;
        foreach (var creditTransaction in account.Transactions
                     .Where(x =>
                         x.Type == StoreCreditTransactionType.Credit &&
                         x.ExpiresAtUtc.HasValue &&
                         x.ExpiresAtUtc.Value <= utcNow &&
                         !x.IsExpired)
                     .ToList())
        {
            var amount = creditTransaction.AmountDelta;
            if (amount <= 0m)
            {
                continue;
            }

            try
            {
                account.PostTransaction(
                    StoreCreditTransactionType.Expire,
                    -amount,
                    $"expire-credit-{creditTransaction.Id}",
                    CustomerAccountReferenceType.None,
                    creditTransaction.Id,
                    "Store credit expired.");

                creditTransaction.MarkExpired();
                expiredTotal += amount;
            }
            catch (InvalidOperationException)
            {
                // Balance may already be reduced; skip.
            }
        }

        if (expiredTotal > 0m)
        {
            await repository.UpdateAccountAsync(account, cancellationToken).ConfigureAwait(false);
        }

        return Result.Success(expiredTotal);
    }

    private async Task<StoreCreditAccount> GetOrCreateAccountAsync(
        int customerId,
        int storeId,
        string currencyCode,
        CancellationToken cancellationToken)
    {
        var account = await repository
            .GetAccountWithTransactionsAsync(customerId, storeId, currencyCode, cancellationToken)
            .ConfigureAwait(false);

        if (account is not null)
        {
            return account;
        }

        account = StoreCreditAccount.Create(customerId, storeId, currencyCode);
        await repository.AddAccountAsync(account, cancellationToken).ConfigureAwait(false);
        return account;
    }

    private async Task<IReadOnlyList<StoreCreditAccount>> ListAccountsForCustomerStoreAsync(
        int customerId,
        int storeId,
        CancellationToken cancellationToken)
    {
        // Single currency account per store for now; extend when multi-currency wallets are needed.
        var account = await repository
            .GetAccountWithTransactionsAsync(customerId, storeId, "USD", cancellationToken)
            .ConfigureAwait(false);

        return account is null ? [] : [account];
    }
}
