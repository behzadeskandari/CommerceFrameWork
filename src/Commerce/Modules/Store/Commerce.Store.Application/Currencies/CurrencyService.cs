using Commerce.Framework.Core.Errors;
using Commerce.Framework.Core.Results;
using Commerce.Store.Application.Abstractions;
using Commerce.Store.Contracts.Currencies;
using Commerce.Store.Domain.Entities;

namespace Commerce.Store.Application.Currencies;

public sealed class CurrencyService(IStoreCurrencyRepository currencyRepository) : ICurrencyService
{
    public async Task<Result<CurrencyDto>> CreateAsync(
        CreateCurrencyRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            var normalizedCode = request.Code.Trim().ToUpperInvariant();
            if (await currencyRepository.GetByCodeAsync(normalizedCode, cancellationToken).ConfigureAwait(false) is not null)
            {
                return Result.Failure<CurrencyDto>(
                    Error.Conflict($"Currency code '{request.Code}' already exists."));
            }

            var currency = StoreCurrency.Create(
                request.Code,
                request.Name,
                request.Symbol ?? request.Code,
                request.DisplayName ?? request.Name,
                request.Rate,
                request.DecimalPlaces,
                request.DisplayOrder,
                request.IsActive);

            await currencyRepository.AddAsync(currency, cancellationToken).ConfigureAwait(false);
            return Result.Success(Map(currency));
        }
        catch (ArgumentException ex)
        {
            return Result.Failure<CurrencyDto>(Error.Validation(ex.Message));
        }
    }

    public async Task<Result<CurrencyDto>> UpdateAsync(
        int currencyId,
        UpdateCurrencyRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var currency = await currencyRepository.GetByIdAsync(currencyId, cancellationToken).ConfigureAwait(false);
        if (currency is null)
        {
            return Result.Failure<CurrencyDto>(Error.NotFound($"Currency '{currencyId}' was not found."));
        }

        try
        {
            currency.Update(
                request.Name,
                request.Symbol ?? currency.Code,
                request.DisplayName ?? request.Name,
                request.Rate,
                request.DecimalPlaces,
                request.DisplayOrder,
                request.IsActive);

            await currencyRepository.UpdateAsync(currency, cancellationToken).ConfigureAwait(false);
            return Result.Success(Map(currency));
        }
        catch (ArgumentException ex)
        {
            return Result.Failure<CurrencyDto>(Error.Validation(ex.Message));
        }
    }

    public async Task<Result<CurrencyDto>> GetByIdAsync(int currencyId, CancellationToken cancellationToken = default)
    {
        var currency = await currencyRepository.GetByIdAsync(currencyId, cancellationToken).ConfigureAwait(false);
        if (currency is null)
        {
            return Result.Failure<CurrencyDto>(Error.NotFound($"Currency '{currencyId}' was not found."));
        }

        return Result.Success(Map(currency));
    }

    public async Task<Result<IReadOnlyList<CurrencyDto>>> ListAsync(
        bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var currencies = await currencyRepository.ListAsync(includeInactive, cancellationToken).ConfigureAwait(false);
        return Result.Success<IReadOnlyList<CurrencyDto>>(currencies.Select(Map).ToList());
    }

    internal static CurrencyDto Map(StoreCurrency currency) =>
        new(
            currency.Id,
            currency.Code,
            currency.Name,
            currency.Symbol,
            currency.DisplayName,
            currency.DecimalPlaces,
            currency.Rate,
            currency.IsActive,
            currency.DisplayOrder,
            currency.CreatedAtUtc,
            currency.UpdatedAtUtc);
}
