using Commerce.Framework.Contracts.Configuration;
using Commerce.Cart.Application;

namespace Commerce.Cart.Infrastructure.Configuration;

public sealed class CartSettingDefinitionProvider : ISettingDefinitionProvider
{
    public IReadOnlyList<SettingDefinition> GetDefinitions() =>
    [
        new(CartSettingKeys.MaxItemQuantity, "Maximum quantity per cart line.", SettingValueType.Integer, "999", "Commerce.Cart"),
        new(CartSettingKeys.MaxDistinctItems, "Maximum distinct offers in a cart.", SettingValueType.Integer, "100", "Commerce.Cart"),
        new(CartSettingKeys.GuestExpirationHours, "Guest cart expiration in hours.", SettingValueType.Integer, "720", "Commerce.Cart"),
        new(CartSettingKeys.CustomerExpirationDays, "Customer cart expiration in days.", SettingValueType.Integer, "30", "Commerce.Cart")
    ];
}
