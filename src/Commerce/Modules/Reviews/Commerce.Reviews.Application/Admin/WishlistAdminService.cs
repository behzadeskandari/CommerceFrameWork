using Commerce.Catalog.Contracts.Products;
using Commerce.Customers.Contracts.Customers;
using Commerce.Framework.Core.Errors;
using Commerce.Framework.Core.Results;
using Commerce.Reviews.Application.Abstractions;
using Commerce.Reviews.Contracts.Admin;

namespace Commerce.Reviews.Application.Admin;

public sealed class WishlistAdminService(
    IReviewsRepository reviewsRepository,
    IProductReader productReader,
    ICustomerReader customerReader) : IWishlistAdminService
{
    public async Task<Result<AdminWishlistListDto>> ListAsync(
        int? storeId,
        int? customerId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var criteria = new WishlistListCriteria(storeId, customerId, page, pageSize);
        var (items, totalCount) = await reviewsRepository
            .ListWishlistsAsync(criteria, cancellationToken)
            .ConfigureAwait(false);

        var summaries = new List<AdminWishlistSummaryDto>();
        foreach (var wishlist in items)
        {
            string? customerName = null;
            var customerResult = await customerReader.GetByIdAsync(wishlist.CustomerId, cancellationToken).ConfigureAwait(false);
            if (customerResult.IsSuccess)
            {
                var customer = customerResult.Value!;
                customerName = $"{customer.FirstName} {customer.LastName}".Trim();
            }

            summaries.Add(new AdminWishlistSummaryDto(
                wishlist.Id,
                wishlist.CustomerId,
                customerName,
                wishlist.StoreId,
                wishlist.Items.Count,
                wishlist.Items.Count > 0 ? wishlist.Items.Max(x => x.AddedAtUtc) : null));
        }

        return Result.Success(new AdminWishlistListDto(summaries, totalCount, page, pageSize));
    }

    public async Task<Result<AdminWishlistDetailDto>> GetAsync(int wishlistId, CancellationToken cancellationToken = default)
    {
        var wishlist = await reviewsRepository.GetWishlistByIdAsync(wishlistId, cancellationToken).ConfigureAwait(false);
        if (wishlist is null)
        {
            return Result.Failure<AdminWishlistDetailDto>(Error.NotFound("Wishlist not found."));
        }

        string? customerName = null;
        var customerResult = await customerReader.GetByIdAsync(wishlist.CustomerId, cancellationToken).ConfigureAwait(false);
        if (customerResult.IsSuccess)
        {
            var customer = customerResult.Value!;
            customerName = $"{customer.FirstName} {customer.LastName}".Trim();
        }

        var items = new List<AdminWishlistItemDto>();
        foreach (var item in wishlist.Items.OrderByDescending(x => x.AddedAtUtc))
        {
            string? productName = null;
            var productResult = await productReader.GetByIdAsync(item.ProductId, cancellationToken).ConfigureAwait(false);
            if (productResult.IsSuccess)
            {
                productName = productResult.Value!.Name;
            }

            items.Add(new AdminWishlistItemDto(item.ProductId, productName, item.AddedAtUtc));
        }

        return Result.Success(new AdminWishlistDetailDto(
            wishlist.Id,
            wishlist.CustomerId,
            customerName,
            wishlist.StoreId,
            items));
    }
}
