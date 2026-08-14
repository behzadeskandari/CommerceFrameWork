using Commerce.Reviews.Application.Abstractions;
using Commerce.Reviews.Application.Rating;
using Commerce.Reviews.Domain.Entities;
using Commerce.Reviews.Domain.Enums;
using Commerce.Framework.Data.Db;
using Microsoft.EntityFrameworkCore;

namespace Commerce.Reviews.Infrastructure.Persistence.Repositories;

public sealed class EfReviewsRepository(CommerceDbContext dbContext) : IReviewsRepository
{
    public Task<ProductReview?> GetReviewByIdAsync(int reviewId, CancellationToken cancellationToken = default) =>
        dbContext.Set<ProductReview>().FirstOrDefaultAsync(x => x.Id == reviewId, cancellationToken);

    public Task<ProductReview?> GetReviewByProductAndCustomerAsync(
        int productId,
        int customerId,
        int storeId,
        CancellationToken cancellationToken = default) =>
        dbContext.Set<ProductReview>()
            .FirstOrDefaultAsync(
                x => x.ProductId == productId && x.CustomerId == customerId && x.StoreId == storeId,
                cancellationToken);

    public async Task AddReviewAsync(ProductReview review, CancellationToken cancellationToken = default)
    {
        dbContext.Set<ProductReview>().Add(review);
        await Task.CompletedTask;
    }

    public async Task SaveReviewAsync(ProductReview review, CancellationToken cancellationToken = default)
    {
        dbContext.Set<ProductReview>().Update(review);
        await Task.CompletedTask;
    }

    public async Task DeleteReviewAsync(ProductReview review, CancellationToken cancellationToken = default)
    {
        dbContext.Set<ProductReview>().Remove(review);
        await Task.CompletedTask;
    }

    public async Task<(IReadOnlyList<ProductReview> Items, int TotalCount)> ListReviewsAsync(
        ReviewListCriteria criteria,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Set<ProductReview>().AsQueryable();

        if (criteria.StoreId.HasValue)
        {
            query = query.Where(x => x.StoreId == criteria.StoreId.Value);
        }

        if (criteria.ProductId.HasValue)
        {
            query = query.Where(x => x.ProductId == criteria.ProductId.Value);
        }

        if (criteria.CustomerId.HasValue)
        {
            query = query.Where(x => x.CustomerId == criteria.CustomerId.Value);
        }

        if (criteria.ModerationStatus.HasValue)
        {
            query = query.Where(x => x.ModerationStatus == criteria.ModerationStatus.Value);
        }
        else if (criteria.ApprovedOnly)
        {
            query = query.Where(x => x.ModerationStatus == ReviewModerationStatus.Approved);
        }

        var totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var page = Math.Max(1, criteria.Page);
        var pageSize = Math.Clamp(criteria.PageSize, 1, 100);

        var items = await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return (items, totalCount);
    }

    public async Task<ProductRatingAggregate> GetRatingAggregateAsync(
        int productId,
        int storeId,
        CancellationToken cancellationToken = default)
    {
        var ratings = await dbContext.Set<ProductReview>()
            .Where(x =>
                x.ProductId == productId &&
                x.StoreId == storeId &&
                x.ModerationStatus == ReviewModerationStatus.Approved)
            .Select(x => x.Rating)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var (average, distribution) = ProductRatingCalculator.Compute(ratings);
        return new ProductRatingAggregate(average, ratings.Count, distribution);
    }

    public Task<Wishlist?> GetWishlistByCustomerAsync(
        int customerId,
        int storeId,
        CancellationToken cancellationToken = default) =>
        dbContext.Set<Wishlist>()
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.CustomerId == customerId && x.StoreId == storeId, cancellationToken);

    public Task<Wishlist?> GetWishlistByIdAsync(int wishlistId, CancellationToken cancellationToken = default) =>
        dbContext.Set<Wishlist>()
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == wishlistId, cancellationToken);

    public async Task AddWishlistAsync(Wishlist wishlist, CancellationToken cancellationToken = default)
    {
        dbContext.Set<Wishlist>().Add(wishlist);
        await Task.CompletedTask;
    }

    public async Task SaveWishlistAsync(Wishlist wishlist, CancellationToken cancellationToken = default)
    {
        dbContext.Set<Wishlist>().Update(wishlist);
        await Task.CompletedTask;
    }

    public async Task<(IReadOnlyList<Wishlist> Items, int TotalCount)> ListWishlistsAsync(
        WishlistListCriteria criteria,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Set<Wishlist>().Include(x => x.Items).AsQueryable();

        if (criteria.StoreId.HasValue)
        {
            query = query.Where(x => x.StoreId == criteria.StoreId.Value);
        }

        if (criteria.CustomerId.HasValue)
        {
            query = query.Where(x => x.CustomerId == criteria.CustomerId.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var page = Math.Max(1, criteria.Page);
        var pageSize = Math.Clamp(criteria.PageSize, 1, 100);

        var items = await query
            .OrderByDescending(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return (items, totalCount);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
