using Commerce.Framework.Domain.ValueObjects;
using CurrencyValue = Commerce.Framework.Domain.ValueObjects.Currency;

namespace Commerce.Framework.Contracts.Currency;

public interface ICurrencyExchangeRateProvider
{
    Task<decimal?> GetRateAsync(
        CurrencyValue sourceCurrency,
        CurrencyValue targetCurrency,
        int? storeId = null,
        CancellationToken cancellationToken = default);
}

public interface ICurrencyConverter
{
    Task<CurrencyConversionResult> ConvertAsync(
        Money sourceAmount,
        CurrencyValue targetCurrency,
        int? storeId = null,
        CancellationToken cancellationToken = default);
}

public sealed record CurrencyConversionResult(
    Money SourceAmount,
    Money ConvertedAmount,
    decimal Rate);
