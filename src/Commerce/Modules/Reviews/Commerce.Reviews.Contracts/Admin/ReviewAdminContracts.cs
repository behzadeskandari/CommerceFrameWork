using Commerce.Framework.Core.Results;
using Commerce.Reviews.Domain.Enums;

namespace Commerce.Reviews.Contracts.Admin;

public sealed record AdminProductReviewDto(
    int Id,
    int ProductId,
    string? ProductName,
    int CustomerId,
    string? CustomerDisplayName,
    int StoreId,
    int Rating,
    string Title,
    string Content,
    ReviewModerationStatus ModerationStatus,
    bool IsVerifiedPurchase,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public sealed record AdminReviewListDto(
    IReadOnlyList<AdminProductReviewDto> Items,
    int TotalCount,
    int Page,
    int PageSize);

public interface IReviewAdminService
{
    Task<Result<AdminReviewListDto>> ListAsync(
        int? storeId,
        int? productId,
        ReviewModerationStatus? moderationStatus,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<Result<AdminProductReviewDto>> GetAsync(int reviewId, CancellationToken cancellationToken = default);

    Task<Result> ApproveAsync(int reviewId, CancellationToken cancellationToken = default);

    Task<Result> RejectAsync(int reviewId, CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(int reviewId, CancellationToken cancellationToken = default);
}
