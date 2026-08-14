using Commerce.Cms.Domain.Enums;
using Commerce.Framework.Core.Entities;

namespace Commerce.Cms.Domain.Entities;

public sealed class WidgetZone : AggregateRoot
{
    public const int SystemNameMaxLength = 100;
    public const int NameMaxLength = 200;

    public string SystemName { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public int DisplayOrder { get; private set; }

    public static WidgetZone Create(string systemName, string name, string? description, int displayOrder)
    {
        if (!WidgetZoneNames.All.Contains(systemName, StringComparer.OrdinalIgnoreCase))
        {
            throw new ArgumentException($"Unknown widget zone '{systemName}'.", nameof(systemName));
        }

        return new WidgetZone
        {
            SystemName = systemName.Trim().ToLowerInvariant(),
            Name = name.Trim(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            DisplayOrder = displayOrder
        };
    }
}

public sealed class WidgetInstance : AggregateRoot
{
    public const int ConfigurationMaxLength = 4000;

    public int StoreId { get; private set; }

    public int WidgetZoneId { get; private set; }

    public WidgetType WidgetType { get; private set; }

    public string ConfigurationJson { get; private set; } = "{}";

    public int? LanguageId { get; private set; }

    public int DisplayOrder { get; private set; }

    public bool IsActive { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public static WidgetInstance Create(
        int storeId,
        int widgetZoneId,
        WidgetType widgetType,
        string configurationJson,
        int? languageId,
        int displayOrder,
        bool isActive = true)
    {
        if (storeId <= 0 || widgetZoneId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(storeId));
        }

        var now = DateTime.UtcNow;
        return new WidgetInstance
        {
            StoreId = storeId,
            WidgetZoneId = widgetZoneId,
            WidgetType = widgetType,
            ConfigurationJson = NormalizeConfig(configurationJson),
            LanguageId = languageId,
            DisplayOrder = displayOrder,
            IsActive = isActive,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
    }

    public void Update(WidgetType widgetType, string configurationJson, int? languageId, int displayOrder, bool isActive)
    {
        WidgetType = widgetType;
        ConfigurationJson = NormalizeConfig(configurationJson);
        LanguageId = languageId;
        DisplayOrder = displayOrder;
        IsActive = isActive;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    private static string NormalizeConfig(string configurationJson)
    {
        if (string.IsNullOrWhiteSpace(configurationJson))
        {
            return "{}";
        }

        var trimmed = configurationJson.Trim();
        return trimmed.Length > ConfigurationMaxLength ? trimmed[..ConfigurationMaxLength] : trimmed;
    }
}
