using Commerce.Framework.Data.Db;
using Commerce.Payments.Application.Abstractions;
using Commerce.Payments.Domain.Entities;
using Commerce.Payments.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Commerce.Payments.Infrastructure.Persistence.Repositories;

public sealed class EfGiftCardRepository(CommerceDbContext dbContext) : IGiftCardRepository
{
    public Task<GiftCard?> GetByIdWithTransactionsAsync(int id, CancellationToken cancellationToken = default) =>
        dbContext.Set<GiftCard>()
            .Include(x => x.Transactions)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

    public Task<GiftCard?> GetByCodeWithTransactionsAsync(string code, CancellationToken cancellationToken = default) =>
        dbContext.Set<GiftCard>()
            .Include(x => x.Transactions)
            .FirstOrDefaultAsync(x => x.Code == code && !x.IsDeleted, cancellationToken);

    public async Task<IReadOnlyList<GiftCard>> ListAsync(int? storeId, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Set<GiftCard>().AsNoTracking().Where(x => !x.IsDeleted);
        if (storeId.HasValue)
        {
            query = query.Where(x => x.StoreId == storeId.Value);
        }

        return await query.OrderByDescending(x => x.CreatedAtUtc).ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task AddAsync(GiftCard giftCard, CancellationToken cancellationToken = default)
    {
        dbContext.Set<GiftCard>().Add(giftCard);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateAsync(GiftCard giftCard, CancellationToken cancellationToken = default)
    {
        dbContext.Set<GiftCard>().Update(giftCard);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> TryRedeemAsync(
        int giftCardId,
        decimal amount,
        int orderId,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        var giftCard = await dbContext.Set<GiftCard>()
            .Include(x => x.Transactions)
            .FirstOrDefaultAsync(x => x.Id == giftCardId && !x.IsDeleted, cancellationToken)
            .ConfigureAwait(false);

        if (giftCard is null)
        {
            return false;
        }

        var existing = giftCard.Transactions.FirstOrDefault(x =>
            string.Equals(x.IdempotencyKey, idempotencyKey.Trim(), StringComparison.Ordinal));

        if (existing is not null)
        {
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return true;
        }

        if (giftCard.Balance < amount)
        {
            return false;
        }

        try
        {
            giftCard.PostTransaction(
                GiftCardTransactionType.Redeem,
                -amount,
                idempotencyKey.Trim(),
                GiftCardReferenceType.Order,
                orderId,
                $"Redeemed for order {orderId}.");

            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            return false;
        }
        catch (InvalidOperationException)
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            return false;
        }
    }
}
