using Commerce.Payments.Contracts.Payments;



namespace Commerce.Payments.Application.Payments;



public sealed class PaymentProviderResolver(IEnumerable<IPaymentProvider> providers)

{

    public IPaymentProvider Resolve(string providerSystemName)

    {

        var provider = providers.FirstOrDefault(x =>

            string.Equals(x.ProviderSystemName, providerSystemName, StringComparison.OrdinalIgnoreCase));



        if (provider is null)

        {

            throw new InvalidOperationException($"Payment provider '{providerSystemName}' is not registered.");

        }



        return provider;

    }



    public bool TryResolve(string providerSystemName, out IPaymentProvider? provider)

    {

        provider = providers.FirstOrDefault(x =>

            string.Equals(x.ProviderSystemName, providerSystemName, StringComparison.OrdinalIgnoreCase));

        return provider is not null;

    }

}

