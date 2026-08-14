using Commerce.Framework.Contracts.Configuration;
using Commerce.Shipping.Application;

namespace Commerce.Shipping.Infrastructure.Configuration;

public sealed class ShippingSettingDefinitionProvider : ISettingDefinitionProvider
{
    public IReadOnlyList<SettingDefinition> GetDefinitions() =>
    [
        new(ShippingSettingKeys.Enabled, "Enable shipping calculation.", SettingValueType.Boolean, "true", "Commerce.Shipping"),
        new(ShippingSettingKeys.DefaultEstimatedDeliveryDays, "Default estimated delivery days.", SettingValueType.Integer, "5", "Commerce.Shipping"),
        new(ShippingSettingKeys.AllowFreeShipping, "Allow free shipping thresholds.", SettingValueType.Boolean, "true", "Commerce.Shipping"),
        new(ShippingSettingKeys.RequireShippingAddress, "Require shipping address for physical products.", SettingValueType.Boolean, "true", "Commerce.Shipping")
    ];
}
