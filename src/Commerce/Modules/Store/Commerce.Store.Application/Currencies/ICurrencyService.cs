using Commerce.Framework.Core.Results;
using Commerce.Store.Application.Currencies;
using Commerce.Store.Contracts.Currencies;

namespace Commerce.Store.Application.Currencies;

public interface ICurrencyService
{
    Task<Result<CurrencyDto>> CreateAsync(
        CreateCurrencyRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<CurrencyDto>> UpdateAsync(
        int currencyId,
        UpdateCurrencyRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<CurrencyDto>> GetByIdAsync(int currencyId, CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<CurrencyDto>>> ListAsync(
        bool includeInactive = false,
        CancellationToken cancellationToken = default);
}
