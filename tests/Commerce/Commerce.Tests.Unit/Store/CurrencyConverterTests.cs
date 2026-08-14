using Commerce.Framework.Domain.ValueObjects;
using Commerce.Store.Domain.Entities;
using Commerce.Store.Infrastructure.Exchange;
using Xunit;
using CurrencyValue = Commerce.Framework.Domain.ValueObjects.Currency;

namespace Commerce.Tests.Unit.Store;

public sealed class CurrencyConverterTests
{
    [Fact]
    public async Task ConvertAsync_SameCurrency_ReturnsOriginalAmountWithRateOne()
    {
        var provider = new FixedExchangeRateProvider(new InMemoryCurrencyRepository());
        var converter = new CurrencyConverter(provider);
        var usd = CurrencyValue.FromCode("USD");
        var money = Money.Create(10m, usd);

        var result = await converter.ConvertAsync(money, usd);

        Assert.Equal(money, result.SourceAmount);
        Assert.Equal(money.Amount, result.ConvertedAmount.Amount);
        Assert.Equal(1m, result.Rate);
    }

    [Fact]
    public async Task ConvertAsync_DifferentCurrencies_UsesExplicitRate()
    {
        var repository = new InMemoryCurrencyRepository();
        repository.Add(StoreCurrency.Create("USD", "US Dollar", "$", "US Dollar", 1m, 2));
        repository.Add(StoreCurrency.Create("EUR", "Euro", "€", "Euro", 0.5m, 2));

        var converter = new CurrencyConverter(new FixedExchangeRateProvider(repository));
        var source = Money.Create(100m, CurrencyValue.FromCode("USD"));

        var result = await converter.ConvertAsync(source, CurrencyValue.FromCode("EUR"));

        Assert.Equal(100m, result.SourceAmount.Amount);
        Assert.Equal(200m, result.ConvertedAmount.Amount);
    }

    private sealed class InMemoryCurrencyRepository : global::Commerce.Store.Application.Abstractions.IStoreCurrencyRepository
    {
        private readonly List<StoreCurrency> _items = [];

        public void Add(StoreCurrency currency) => _items.Add(currency);

        public Task AddAsync(StoreCurrency currency, CancellationToken cancellationToken = default)
        {
            _items.Add(currency);
            return Task.CompletedTask;
        }

        public Task<StoreCurrency?> GetByCodeAsync(string code, CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(x => x.Code.Equals(code, StringComparison.OrdinalIgnoreCase)));

        public Task<StoreCurrency?> GetByIdAsync(int currencyId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(x => x.Id == currencyId));

        public Task<IReadOnlyList<StoreCurrency>> ListAsync(bool includeInactive, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<StoreCurrency>>(_items.ToList());

        public Task UpdateAsync(StoreCurrency currency, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
