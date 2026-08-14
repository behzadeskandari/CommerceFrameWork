using Commerce.Catalog.Contracts.Products;
using Commerce.Framework.Contracts.Tenancy;
using Commerce.Framework.Core.Errors;
using Commerce.Framework.Core.Results;
using Commerce.Reviews.Application.Abstractions;
using Commerce.Reviews.Contracts.Storefront;
using Commerce.Reviews.Domain.Entities;

namespace Commerce.Reviews.Application.Storefront;

public sealed class WishlistStorefrontService(
    IReviewsRepository reviewsRepository,
    IProductReader productReader,
    IStoreContext storeContext) : IWishlistStorefrontService
{
    public async Task<Result<WishlistDto>> GetAsync(int customerId, CancellationToken cancellationToken = default)
    {
        var storeId = RequireStoreId();
        var wishlist = await GetOrCreateWishlistAsync(customerId, storeId, cancellationToken).ConfigureAwait(false);
        var items = await MapItemsAsync(wishlist, cancellationToken).ConfigureAwait(false);

        return Result.Success(new WishlistDto(wishlist.Id, wishlist.CustomerId, wishlist.StoreId, items));
    }

    public async Task<Result<WishlistItemDto>> AddItemAsync(
        int customerId,
        AddWishlistItemRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.ProductId <= 0)
        {
            return Result.Failure<WishlistItemDto>(Error.Validation("Product is required."));
        }

        var storeId = RequireStoreId();
        var productResult = await productReader.GetByIdAsync(request.ProductId, cancellationToken).ConfigureAwait(false);
        if (productResult.IsFailure)
        {
            return Result.Failure<WishlistItemDto>(productResult.Error!);
        }

        var wishlist = await GetOrCreateWishlistAsync(customerId, storeId, cancellationToken).ConfigureAwait(false);
        if (wishlist.ContainsProduct(request.ProductId))
        {
            return Result.Failure<WishlistItemDto>(Error.Conflict("Product is already in your wishlist."));
        }

        var utcNow = DateTime.UtcNow;
        try
        {
            wishlist.AddProduct(request.ProductId, utcNow);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure<WishlistItemDto>(Error.Conflict(ex.Message));
        }

        await reviewsRepository.SaveWishlistAsync(wishlist, cancellationToken).ConfigureAwait(false);
        await reviewsRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var product = productResult.Value!;
        return Result.Success(new WishlistItemDto(
            request.ProductId,
            product.Name,
            product.Slug,
            product.IsAvailable,
            utcNow));
    }

    public async Task<Result> RemoveItemAsync(
        int customerId,
        int productId,
        CancellationToken cancellationToken = default)
    {
        var storeId = RequireStoreId();
        var wishlist = await reviewsRepository
            .GetWishlistByCustomerAsync(customerId, storeId, cancellationToken)
            .ConfigureAwait(false);

        if (wishlist is null || !wishlist.RemoveProduct(productId))
        {
            return Result.Failure(Error.NotFound("Wishlist item not found."));
        }

        await reviewsRepository.SaveWishlistAsync(wishlist, cancellationToken).ConfigureAwait(false);
        await reviewsRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }

    private async Task<Wishlist> GetOrCreateWishlistAsync(
        int customerId,
        int storeId,
        CancellationToken cancellationToken)
    {
        var wishlist = await reviewsRepository
            .GetWishlistByCustomerAsync(customerId, storeId, cancellationToken)
            .ConfigureAwait(false);

        if (wishlist is not null)
        {
            return wishlist;
        }

        wishlist = Wishlist.Create(customerId, storeId);
        await reviewsRepository.AddWishlistAsync(wishlist, cancellationToken).ConfigureAwait(false);
        await reviewsRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return wishlist;
    }

    private async Task<IReadOnlyList<WishlistItemDto>> MapItemsAsync(
        Wishlist wishlist,
        CancellationToken cancellationToken)
    {
        var items = new List<WishlistItemDto>();
        foreach (var item in wishlist.Items.OrderByDescending(x => x.AddedAtUtc))
        {
            var productResult = await productReader.GetByIdAsync(item.ProductId, cancellationToken).ConfigureAwait(false);
            if (productResult.IsFailure)
            {
                continue;
            }

            var product = productResult.Value!;
            items.Add(new WishlistItemDto(
                item.ProductId,
                product.Name,
                product.Slug,
                product.IsAvailable,
                item.AddedAtUtc));
        }

        return items;
    }

    private int RequireStoreId() =>
        storeContext.CurrentStoreId ?? throw new InvalidOperationException("Store context is required.");
}
