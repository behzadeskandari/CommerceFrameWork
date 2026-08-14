using Commerce.Checkout.Contracts.Checkout;

using Commerce.Payments.Application.Abstractions;

using Commerce.Payments.Contracts.Payments;



namespace Commerce.Payments.Application.Payments;



public sealed class PaymentCheckoutMethodProvider(IPaymentRepository repository) : IPaymentMethodProvider

{

    public string ProviderSystemName => PaymentProviderNames.CheckoutBridge;



    public async Task<IReadOnlyList<PaymentMethodDto>> GetMethodsAsync(

        int storeId,

        string currencyCode,

        bool isGuest,

        CancellationToken cancellationToken = default)

    {

        var methods = await repository.GetActiveMethodsAsync(storeId, cancellationToken).ConfigureAwait(false);



        return methods

            .Where(x => !x.IsDeleted && x.IsActive)

            .Where(x => !isGuest || x.SupportsGuest)

            .OrderBy(x => x.DisplayOrder)

            .Select(x => new PaymentMethodDto(

                x.Id.ToString(),

                x.Name,

                x.SystemName,

                x.DisplayName,

                x.RequiresRedirect,

                x.SupportsGuest,

                SupportsCurrency: true))

            .ToList();

    }

}

