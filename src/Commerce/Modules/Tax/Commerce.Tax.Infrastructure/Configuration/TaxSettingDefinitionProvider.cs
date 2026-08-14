using Commerce.Framework.Contracts.Configuration;
using Commerce.Tax.Application;

namespace Commerce.Tax.Infrastructure.Configuration;

public sealed class TaxSettingDefinitionProvider : ISettingDefinitionProvider
{
    public IReadOnlyList<SettingDefinition> GetDefinitions() =>
    [
        new(TaxSettingKeys.Enabled, "Enable tax calculation.", SettingValueType.Boolean, "true", "Commerce.Tax"),
        new(TaxSettingKeys.PricesIncludeTax, "Catalog prices include tax.", SettingValueType.Boolean, "false", "Commerce.Tax"),
        new(TaxSettingKeys.DefaultCategoryId, "Default tax category for products without classification.", SettingValueType.Integer, "", "Commerce.Tax"),
        new(TaxSettingKeys.ShippingTaxableByDefault, "Apply tax to shipping by default.", SettingValueType.Boolean, "true", "Commerce.Tax")
    ];
}
