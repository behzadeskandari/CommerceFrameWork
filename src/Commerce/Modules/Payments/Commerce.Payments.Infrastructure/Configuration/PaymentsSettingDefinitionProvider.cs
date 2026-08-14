using Commerce.Framework.Contracts.Configuration;
using Commerce.Payments.Application;

namespace Commerce.Payments.Infrastructure.Configuration;

public sealed class PaymentsSettingDefinitionProvider : ISettingDefinitionProvider
{
    public IReadOnlyList<SettingDefinition> GetDefinitions() =>
    [
        new(PaymentSettingKeys.Enabled, "Enable payment processing.", SettingValueType.Boolean, "true", "Commerce.Payments")
    ];
}
