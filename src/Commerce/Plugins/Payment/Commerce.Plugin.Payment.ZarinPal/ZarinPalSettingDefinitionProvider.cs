using Commerce.Framework.PluginContracts.Settings;
using Commerce.Payments.Contracts.Payments;

namespace Commerce.Plugin.Payment.ZarinPal;

public sealed class ZarinPalSettingDefinitionProvider : IPluginSettingDefinitionProvider
{
    public string PluginSystemName => PaymentProviderNames.ZarinPal;

    public IReadOnlyList<PluginSettingDefinition> GetDefinitions() =>
    [
        new(
            ZarinPalSettingKeys.MerchantId,
            "ZarinPal merchant UUID.",
            PluginSettingValueType.String,
            string.Empty,
            IsStoreScoped: true,
            IsSecret: false),
        new(
            ZarinPalSettingKeys.Sandbox,
            "Use ZarinPal sandbox environment.",
            PluginSettingValueType.Boolean,
            "true",
            IsStoreScoped: true),
        new(
            ZarinPalSettingKeys.CallbackBaseUrl,
            "Server callback base URL (must include /api/payments/callback/Payment.ZarinPal).",
            PluginSettingValueType.String,
            "https://localhost/api/payments/callback/Payment.ZarinPal",
            IsStoreScoped: true)
    ];
}
