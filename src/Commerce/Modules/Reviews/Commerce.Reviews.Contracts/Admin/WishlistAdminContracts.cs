using Commerce.Framework.Core.Results;

namespace Commerce.Reviews.Contracts.Admin;

public sealed record AdminWishlistSummaryDto(
    int Id,
    int CustomerId,
    string? CustomerDisplayName,
    int StoreId,
    int ItemCount,
    DateTime? LastAddedAtUtc);

public sealed record AdminWishlistListDto(
    IReadOnlyList<AdminWishlistSummaryDto> Items,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record AdminWishlistDetailDto(
    int Id,
    int CustomerId,
    string? CustomerDisplayName,
    int StoreId,
    IReadOnlyList<AdminWishlistItemDto> Items);

public sealed record AdminWishlistItemDto(
    int ProductId,
    string? ProductName,
    DateTime AddedAtUtc);

public interface IWishlistAdminService
{
    Task<Result<AdminWishlistListDto>> ListAsync(
        int? storeId,
        int? customerId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<Result<AdminWishlistDetailDto>> GetAsync(int wishlistId, CancellationToken cancellationToken = default);
}
