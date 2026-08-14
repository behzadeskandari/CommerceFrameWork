using Commerce.Framework.Core.Entities;

namespace Commerce.Seo.Domain.Entities;

public sealed class UrlRecord : AggregateRoot
{
    public const int EntityNameMaxLength = 128;
    public const int SlugMaxLength = 256;

    public string EntityName { get; private set; } = string.Empty;

    public int EntityId { get; private set; }

    public string Slug { get; private set; } = string.Empty;

    public int? LanguageId { get; private set; }

    public int? StoreId { get; private set; }

    public bool IsActive { get; private set; }

    public static UrlRecord Create(string entityName, int entityId, string slug, int? languageId, int? storeId, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(entityName) || entityId <= 0 || string.IsNullOrWhiteSpace(slug))
        {
            throw new ArgumentException("Entity name, id, and slug are required.");
        }

        return new UrlRecord
        {
            EntityName = entityName.Trim(),
            EntityId = entityId,
            Slug = slug.Trim().ToLowerInvariant(),
            LanguageId = languageId,
            StoreId = storeId,
            IsActive = isActive
        };
    }

    public void Update(string slug, bool isActive)
    {
        Slug = slug.Trim().ToLowerInvariant();
        IsActive = isActive;
    }
}
