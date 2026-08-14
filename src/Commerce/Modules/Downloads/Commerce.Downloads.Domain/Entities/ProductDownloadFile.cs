using Commerce.Framework.Core.Entities;

namespace Commerce.Downloads.Domain.Entities;

public sealed class ProductDownloadFile : Entity
{
    public const int DisplayNameMaxLength = 400;

    public int ProductId { get; private set; }

    public int StoreId { get; private set; }

    public int MediaAssetId { get; private set; }

    public string? DisplayName { get; private set; }

    public int DisplayOrder { get; private set; }

    public bool IsActive { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public static ProductDownloadFile Create(
        int productId,
        int storeId,
        int mediaAssetId,
        string? displayName,
        int displayOrder,
        bool isActive = true)
    {
        if (productId <= 0 || storeId <= 0 || mediaAssetId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(productId));
        }

        return new ProductDownloadFile
        {
            ProductId = productId,
            StoreId = storeId,
            MediaAssetId = mediaAssetId,
            DisplayName = NormalizeDisplayName(displayName),
            DisplayOrder = displayOrder,
            IsActive = isActive,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    public void Update(string? displayName, int displayOrder, bool isActive)
    {
        DisplayName = NormalizeDisplayName(displayName);
        DisplayOrder = displayOrder;
        IsActive = isActive;
    }

    private static string? NormalizeDisplayName(string? displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            return null;
        }

        var trimmed = displayName.Trim();
        return trimmed.Length > DisplayNameMaxLength ? trimmed[..DisplayNameMaxLength] : trimmed;
    }
}
