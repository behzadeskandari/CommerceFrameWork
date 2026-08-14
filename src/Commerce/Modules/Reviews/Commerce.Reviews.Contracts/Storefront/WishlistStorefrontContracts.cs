using Commerce.Framework.Core.Results;

namespace Commerce.Reviews.Contracts.Storefront;

public sealed record WishlistItemDto(
    int ProductId,
    string ProductName,
    string? Slug,
    bool IsAvailable,
    DateTime AddedAtUtc);

public sealed record WishlistDto(
    int Id,
    int CustomerId,
    int StoreId,
    IReadOnlyList<WishlistItemDto> Items);

public sealed record AddWishlistItemRequest(int ProductId);

public interface IWishlistStorefrontService
{
    Task<Result<WishlistDto>> GetAsync(int customerId, CancellationToken cancellationToken = default);

    Task<Result<WishlistItemDto>> AddItemAsync(
        int customerId,
        AddWishlistItemRequest request,
        CancellationToken cancellationToken = default);

    Task<Result> RemoveItemAsync(
        int customerId,
        int productId,
        CancellationToken cancellationToken = default);
}
