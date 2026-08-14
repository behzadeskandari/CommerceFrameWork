namespace Commerce.Themes.Application.Abstractions;

using Commerce.Themes.Domain.Entities;

public interface IThemeRepository
{
    Task<StoreThemeConfiguration?> GetByStoreIdAsync(int storeId, CancellationToken cancellationToken = default);

    Task SaveAsync(StoreThemeConfiguration configuration, CancellationToken cancellationToken = default);
}
