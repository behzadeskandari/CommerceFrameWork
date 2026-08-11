using Commerce.Store.Domain.Entities;
using StoreEntity = Commerce.Store.Domain.Entities.Store;

namespace Commerce.Store.Application.Abstractions;

public interface IStoreRepository
{
    Task<StoreEntity?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<StoreEntity?> GetBySystemNameAsync(string systemName, CancellationToken cancellationToken = default);

    Task<StoreEntity?> GetDefaultActiveAsync(CancellationToken cancellationToken = default);

    Task<StoreEntity?> FindByHostAsync(string host, int? port, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StoreEntity>> ListAsync(bool includeInactive, CancellationToken cancellationToken = default);

    Task AddAsync(StoreEntity store, CancellationToken cancellationToken = default);

    Task UpdateAsync(StoreEntity store, CancellationToken cancellationToken = default);
}

public interface ILanguageRepository
{
    Task<Language?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<Language?> GetByLanguageCodeAsync(string languageCode, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Language>> ListAsync(bool includeInactive, CancellationToken cancellationToken = default);

    Task AddAsync(Language language, CancellationToken cancellationToken = default);

    Task UpdateAsync(Language language, CancellationToken cancellationToken = default);
}

public interface IStoreCurrencyRepository
{
    Task<StoreCurrency?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<StoreCurrency?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StoreCurrency>> ListAsync(bool includeInactive, CancellationToken cancellationToken = default);

    Task AddAsync(StoreCurrency currency, CancellationToken cancellationToken = default);

    Task UpdateAsync(StoreCurrency currency, CancellationToken cancellationToken = default);
}
