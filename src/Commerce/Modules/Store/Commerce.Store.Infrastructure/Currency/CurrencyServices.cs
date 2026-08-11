using Commerce.Framework.Contracts.Currency;
using Commerce.Framework.Domain.ValueObjects;
using Commerce.Store.Application.Abstractions;
using CurrencyValue = Commerce.Framework.Domain.ValueObjects.Currency;

namespace Commerce.Store.Infrastructure.Exchange;

public sealed class FixedExchangeRateProvider(IStoreCurrencyRepository currencyRepository) : ICurrencyExchangeRateProvider
{
    public async Task<decimal?> GetRateAsync(
        CurrencyValue sourceCurrency,
        CurrencyValue targetCurrency,
        int? storeId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sourceCurrency);
        ArgumentNullException.ThrowIfNull(targetCurrency);

        _ = storeId;

        if (sourceCurrency.Code.Equals(targetCurrency.Code, StringComparison.OrdinalIgnoreCase))
        {
            return 1m;
        }

        var source = await currencyRepository.GetByCodeAsync(sourceCurrency.Code, cancellationToken).ConfigureAwait(false);
        var target = await currencyRepository.GetByCodeAsync(targetCurrency.Code, cancellationToken).ConfigureAwait(false);

        if (source is null || target is null || !source.IsActive || !target.IsActive)
        {
            return null;
        }

        if (source.Rate <= 0 || target.Rate <= 0)
        {
            return null;
        }

        return source.Rate / target.Rate;
    }
}

public sealed class CurrencyConverter(ICurrencyExchangeRateProvider exchangeRateProvider) : ICurrencyConverter
{
    public async Task<CurrencyConversionResult> ConvertAsync(
        Money sourceAmount,
        CurrencyValue targetCurrency,
        int? storeId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sourceAmount);
        ArgumentNullException.ThrowIfNull(targetCurrency);

        if (sourceAmount.Currency.Code.Equals(targetCurrency.Code, StringComparison.OrdinalIgnoreCase))
        {
            return new CurrencyConversionResult(sourceAmount, sourceAmount, 1m);
        }

        var rate = await exchangeRateProvider
            .GetRateAsync(sourceAmount.Currency, targetCurrency, storeId, cancellationToken)
            .ConfigureAwait(false);

        if (!rate.HasValue)
        {
            throw new InvalidOperationException(
                $"No exchange rate available from {sourceAmount.Currency.Code} to {targetCurrency.Code}.");
        }

        var converted = Money.Create(sourceAmount.Amount * rate.Value, targetCurrency);
        return new CurrencyConversionResult(sourceAmount, converted, rate.Value);
    }
}
