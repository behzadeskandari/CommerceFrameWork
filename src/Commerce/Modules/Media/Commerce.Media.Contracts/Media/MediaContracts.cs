using Commerce.Framework.Core.Results;
using Commerce.Media.Domain.Enums;

namespace Commerce.Media.Contracts.Media;

public sealed record MediaSummaryDto(
    int Id,
    int StoreId,
    string FileName,
    string OriginalFileName,
    string ContentType,
    string Extension,
    long Size,
    string MediaType,
    bool IsPublic,
    int? Width,
    int? Height,
    string? Title,
    string? AltText,
    string Url,
    string? ThumbnailUrl,
    DateTime CreatedAtUtc);

public sealed record MediaAssetDto(
    int Id,
    int StoreId,
    string FileName,
    string OriginalFileName,
    string ContentType,
    string Extension,
    long Size,
    string MediaType,
    bool IsPublic,
    bool IsDeleted,
    int? Width,
    int? Height,
    string? Title,
    string? AltText,
    string? ContentHash,
    string Url,
    string? ThumbnailUrl,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public sealed record UploadMediaRequest(
    Stream Content,
    string OriginalFileName,
    string? ContentType,
    long ContentLength,
    bool IsPublic = true);

public sealed record UpdateMediaRequest(
    string? Title,
    string? AltText,
    bool IsPublic);

public interface IMediaReader
{
    Task<Result<MediaAssetDto>> GetByIdAsync(int mediaId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MediaSummaryDto>> GetByIdsAsync(
        IReadOnlyCollection<int> mediaIds,
        CancellationToken cancellationToken = default);

    Task<Result<MediaAssetDto>> GetForStoreAsync(
        int mediaId,
        int storeId,
        CancellationToken cancellationToken = default);
}

public interface IMediaService : IMediaReader
{
    Task<Result<IReadOnlyList<MediaSummaryDto>>> ListAsync(
        string? term = null,
        MediaType? mediaType = null,
        CancellationToken cancellationToken = default);

    Task<Result<MediaAssetDto>> UploadAsync(
        UploadMediaRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<MediaAssetDto>> UpdateAsync(
        int mediaId,
        UpdateMediaRequest request,
        CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(int mediaId, CancellationToken cancellationToken = default);

    Task<Result<Stream>> OpenPublicReadAsync(
        int mediaId,
        bool thumbnail = false,
        CancellationToken cancellationToken = default);

    Task<Result<Stream>> OpenAuthorizedReadAsync(
        int mediaId,
        bool thumbnail = false,
        CancellationToken cancellationToken = default);
}
