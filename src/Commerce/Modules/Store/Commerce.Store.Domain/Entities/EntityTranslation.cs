using Commerce.Framework.Core.Entities;

namespace Commerce.Store.Domain.Entities;

public sealed class EntityTranslation : Entity
{
    public const int EntityTypeMaxLength = 100;
    public const int PropertyMaxLength = 100;
    public const int ValueMaxLength = 4000;

    private EntityTranslation()
    {
    }

    public string EntityType { get; private set; } = string.Empty;

    public int EntityId { get; private set; }

    public int LanguageId { get; private set; }

    public string Property { get; private set; } = string.Empty;

    public string Value { get; private set; } = string.Empty;

    public static EntityTranslation Create(
        string entityType,
        int entityId,
        int languageId,
        string property,
        string value)
    {
        if (string.IsNullOrWhiteSpace(entityType))
        {
            throw new ArgumentException("Entity type is required.", nameof(entityType));
        }

        if (entityId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(entityId));
        }

        if (languageId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(languageId));
        }

        if (string.IsNullOrWhiteSpace(property))
        {
            throw new ArgumentException("Property is required.", nameof(property));
        }

        return new EntityTranslation
        {
            EntityType = entityType.Trim(),
            EntityId = entityId,
            LanguageId = languageId,
            Property = property.Trim(),
            Value = value ?? string.Empty
        };
    }

    public void UpdateValue(string value) => Value = value ?? string.Empty;
}
