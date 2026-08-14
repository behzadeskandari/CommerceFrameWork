using Commerce.Framework.Contracts.Configuration;
using Commerce.Framework.PluginContracts.Settings;

namespace Commerce.Framework.Plugins.Settings;

public sealed class PluginSettingDefinitionAggregator(IEnumerable<IPluginSettingDefinitionProvider> providers)
    : ISettingDefinitionProvider
{
    public IReadOnlyList<SettingDefinition> GetDefinitions()
    {
        return providers
            .SelectMany(provider => provider.GetDefinitions().Select(definition => new SettingDefinition(
                definition.Key,
                definition.Description,
                MapValueType(definition.ValueType),
                definition.DefaultValue,
                provider.PluginSystemName)))
            .ToList();
    }

    private static SettingValueType MapValueType(PluginSettingValueType valueType) =>
        valueType switch
        {
            PluginSettingValueType.Boolean => SettingValueType.Boolean,
            PluginSettingValueType.Integer => SettingValueType.Integer,
            PluginSettingValueType.Decimal => SettingValueType.Decimal,
            PluginSettingValueType.Secret => SettingValueType.String,
            _ => SettingValueType.String
        };
}

public sealed class PluginSettingSecretRegistry
{
    private readonly HashSet<string> _secretKeys = new(StringComparer.OrdinalIgnoreCase);

    public PluginSettingSecretRegistry(IEnumerable<IPluginSettingDefinitionProvider> providers)
    {
        foreach (var provider in providers)
        {
            foreach (var definition in provider.GetDefinitions().Where(x => x.IsSecret))
            {
                _secretKeys.Add(definition.Key);
            }
        }
    }

    public bool IsSecret(string key) => _secretKeys.Contains(key);
}
