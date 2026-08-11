using Commerce.Media.Domain.Entities;

namespace Commerce.Media.Application.Abstractions;

public interface IMediaAssetRepository
{
    Task<MediaAsset?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MediaAsset>> GetByIdsAsync(IReadOnlyCollection<int> ids, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MediaAsset>> ListAsync(
        int storeId,
        string? term = null,
        Domain.Enums.MediaType? mediaType = null,
        CancellationToken cancellationToken = default);

    Task AddAsync(MediaAsset asset, CancellationToken cancellationToken = default);

    Task UpdateAsync(MediaAsset asset, CancellationToken cancellationToken = default);
}
