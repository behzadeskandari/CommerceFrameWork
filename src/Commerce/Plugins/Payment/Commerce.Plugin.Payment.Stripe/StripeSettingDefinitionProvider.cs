using Commerce.Framework.PluginContracts.Settings;
using Commerce.Payments.Contracts.Payments;

namespace Commerce.Plugin.Payment.Stripe;

public sealed class StripeSettingDefinitionProvider : IPluginSettingDefinitionProvider
{
    public string PluginSystemName => PaymentProviderNames.Stripe;

    public IReadOnlyList<PluginSettingDefinition> GetDefinitions() =>
    [
        new(
            StripeSettingKeys.SecretKey,
            "Stripe secret API key.",
            PluginSettingValueType.Secret,
            string.Empty,
            IsStoreScoped: true,
            IsSecret: true),
        new(
            StripeSettingKeys.WebhookSecret,
            "Stripe webhook signing secret.",
            PluginSettingValueType.Secret,
            string.Empty,
            IsStoreScoped: true,
            IsSecret: true),
        new(
            StripeSettingKeys.Sandbox,
            "Use Stripe test mode keys.",
            PluginSettingValueType.Boolean,
            "true",
            IsStoreScoped: true)
    ];
}
