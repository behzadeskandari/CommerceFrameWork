using Commerce.SmartstoreImport.Contracts;
using Commerce.SmartstoreImport.Domain.Enums;

namespace Commerce.SmartstoreImport.Application.Abstractions;

public interface ISmartstoreEntityImporter
{
    int Order { get; }

    string EntityType { get; }

    IReadOnlyList<string> SourceTables { get; }

    bool CanImport(SmartstoreParsedDataSet dataSet);

    Task<SmartstoreEntityImportSummary> ImportAsync(SmartstoreImportContext context, CancellationToken cancellationToken = default);
}

public sealed class SmartstoreImportContext
{
    public required SmartstoreParsedDataSet DataSet { get; init; }

    public required int ImportRunId { get; init; }

    public required SmartstoreImportOptions Options { get; init; }

    public required IImportIdRegistry IdRegistry { get; init; }

    public required IImportIssueReporter Issues { get; init; }

    public required IServiceProvider Services { get; init; }

    public int SyntheticCheckoutBase { get; init; } = 900_000_000;

    public int SyntheticCartBase { get; init; } = 800_000_000;

    public int SyntheticCartItemBase { get; init; } = 700_000_000;
}

public interface IImportIdRegistry
{
    bool TryGetTargetId(string entityType, int sourceId, out int targetId);

    void Register(string entityType, int sourceId, int targetId, string? sourceKey = null);

    Task PersistAsync(CancellationToken cancellationToken = default);

    Task LoadExistingAsync(int? importRunId, CancellationToken cancellationToken = default);
}

public interface IImportIssueReporter
{
    void Warning(string entityType, int? sourceId, string code, string message, string? details = null);

    void Error(string entityType, int? sourceId, string code, string message, string? details = null);

    IReadOnlyList<SmartstoreImportIssueDto> GetIssues();

    int WarningCount { get; }

    int ErrorCount { get; }
}

public static class SmartstoreImportTableNames
{
    public const string Store = "Store";
    public const string StoreMapping = "StoreMapping";
    public const string Language = "Language";
    public const string Currency = "Currency";
    public const string Setting = "Setting";
    public const string Customer = "Customer";
    public const string CustomerRole = "CustomerRole";
    public const string CustomerRoleMapping = "CustomerRoleMapping";
    public const string Address = "Address";
    public const string Category = "Category";
    public const string Manufacturer = "Manufacturer";
    public const string Product = "Product";
    public const string ProductAttribute = "ProductAttribute";
    public const string ProductAttributeOption = "ProductAttributeOption";
    public const string ProductVariantAttributeCombination = "ProductVariantAttributeCombination";
    public const string ProductCategoryMapping = "Product_Category_Mapping";
    public const string ProductManufacturerMapping = "Product_Manufacturer_Mapping";
    public const string ProductMediaMapping = "Product_MediaFile_Mapping";
    public const string MediaFile = "MediaFile";
    public const string MediaFolder = "MediaFolder";
    public const string Order = "Order";
    public const string OrderItem = "OrderItem";
    public const string ProductReview = "ProductReview";
    public const string Topic = "Topic";
    public const string UrlRecord = "UrlRecord";
    public const string LocaleStringResource = "LocaleStringResource";
    public const string LocalizedProperty = "LocalizedProperty";
    public const string Discount = "Discount";
    public const string Download = "Download";
}

public static class SmartstoreImportEntityTypes
{
    public const string Store = "Store";
    public const string Language = "Language";
    public const string Currency = "Currency";
    public const string Setting = "Setting";
    public const string Customer = "Customer";
    public const string Category = "Category";
    public const string Manufacturer = "Manufacturer";
    public const string Product = "Product";
    public const string ProductOffer = "ProductOffer";
    public const string ProductAttribute = "ProductAttribute";
    public const string ProductVariant = "ProductVariant";
    public const string MediaAsset = "MediaAsset";
    public const string Order = "Order";
    public const string OrderItem = "OrderItem";
    public const string ProductReview = "ProductReview";
    public const string Topic = "Topic";
    public const string UrlRecord = "UrlRecord";
    public const string Localization = "Localization";
    public const string Discount = "Discount";
    public const string Download = "Download";
}
