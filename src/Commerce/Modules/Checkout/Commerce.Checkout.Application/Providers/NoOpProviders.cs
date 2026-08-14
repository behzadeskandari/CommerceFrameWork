using Commerce.Checkout.Contracts.Checkout;

namespace Commerce.Checkout.Application.Providers;

public sealed class NoOpTaxCalculator : ITaxCalculator
{
    public Task<TaxCalculationResult> CalculateAsync(
        TaxCalculationRequest request,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new TaxCalculationResult(
            0m,
            0m,
            0m,
            request.CurrencyCode,
            [],
            [],
            false));
}

public sealed class NoOpDiscountCalculator : IDiscountCalculator
{
    public Task<DiscountCalculationResult> CalculateAsync(
        DiscountCalculationRequest request,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new DiscountCalculationResult(0m, request.CurrencyCode, []));
}

public sealed class NoOpPaymentMethodProvider : IPaymentMethodProvider
{
    public string ProviderSystemName => "Commerce.Checkout.NoPayment";

    public Task<IReadOnlyList<PaymentMethodDto>> GetMethodsAsync(
        int storeId,
        string currencyCode,
        bool isGuest,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<PaymentMethodDto>>([]);
}
