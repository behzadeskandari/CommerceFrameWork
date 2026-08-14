using Commerce.Tax.Domain.Entities;

namespace Commerce.Tax.Application.Abstractions;

public interface ITaxRepository
{
    Task<IReadOnlyList<TaxCategory>> GetActiveCategoriesAsync(int storeId, CancellationToken cancellationToken = default);

    Task<TaxCategory?> GetCategoryByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TaxCategory>> ListCategoriesAsync(int? storeId, CancellationToken cancellationToken = default);

    Task AddCategoryAsync(TaxCategory category, CancellationToken cancellationToken = default);

    Task SaveCategoryAsync(TaxCategory category, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TaxZone>> GetActiveZonesAsync(int storeId, CancellationToken cancellationToken = default);

    Task<TaxZone?> GetZoneByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TaxZone>> ListZonesAsync(int? storeId, CancellationToken cancellationToken = default);

    Task AddZoneAsync(TaxZone zone, CancellationToken cancellationToken = default);

    Task SaveZoneAsync(TaxZone zone, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TaxRate>> GetActiveRatesAsync(int storeId, CancellationToken cancellationToken = default);

    Task<TaxRate?> GetRateByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TaxRate>> ListRatesAsync(int? storeId, int? categoryId, CancellationToken cancellationToken = default);

    Task AddRateAsync(TaxRate rate, CancellationToken cancellationToken = default);

    Task SaveRateAsync(TaxRate rate, CancellationToken cancellationToken = default);
}
