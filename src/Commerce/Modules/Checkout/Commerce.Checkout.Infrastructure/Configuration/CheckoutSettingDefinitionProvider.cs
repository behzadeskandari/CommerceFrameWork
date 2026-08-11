using Commerce.Checkout.Application;
using Commerce.Framework.Contracts.Configuration;

namespace Commerce.Checkout.Infrastructure.Configuration;

public sealed class CheckoutSettingDefinitionProvider : ISettingDefinitionProvider
{
    public IReadOnlyList<SettingDefinition> GetDefinitions() =>
    [
        new(
            CheckoutSettingKeys.ExpirationMinutes,
            "Checkout session expiration in minutes.",
            SettingValueType.Integer,
            "60",
            "Commerce.Checkout")
    ];
}
