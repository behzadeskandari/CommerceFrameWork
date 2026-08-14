using Commerce.Cms.Domain.Enums;
using Commerce.Framework.Core.Results;

namespace Commerce.Cms.Contracts.Admin;

public sealed record ContentPageLocalizationDto(
    int Id,
    int LanguageId,
    string Title,
    string Slug,
    string Body,
    string? MetaTitle,
    string? MetaDescription,
    string? MetaKeywords,
    string? CanonicalUrl);

public sealed record ContentPageSummaryDto(
    int Id,
    int StoreId,
    string? SystemName,
    bool IsPublished,
    DateTime? PublishedFromUtc,
    DateTime? PublishedToUtc,
    string? DefaultTitle,
    string? DefaultSlug,
    DateTime UpdatedAtUtc);

public sealed record ContentPageDetailDto(
    int Id,
    int StoreId,
    string? SystemName,
    bool IsPublished,
    DateTime? PublishedFromUtc,
    DateTime? PublishedToUtc,
    IReadOnlyList<ContentPageLocalizationDto> Localizations,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public sealed record ContentPageLocalizationRequest(
    int LanguageId,
    string Title,
    string Slug,
    string Body,
    string? MetaTitle,
    string? MetaDescription,
    string? MetaKeywords,
    string? CanonicalUrl);

public sealed record CreateContentPageRequest(
    int StoreId,
    string? SystemName,
    bool IsPublished,
    DateTime? PublishedFromUtc,
    DateTime? PublishedToUtc,
    IReadOnlyList<ContentPageLocalizationRequest> Localizations);

public sealed record UpdateContentPageRequest(
    string? SystemName,
    bool IsPublished,
    DateTime? PublishedFromUtc,
    DateTime? PublishedToUtc,
    IReadOnlyList<ContentPageLocalizationRequest> Localizations);

public sealed record TopicLocalizationDto(
    int Id,
    int LanguageId,
    string Title,
    string Body,
    string? MetaTitle,
    string? MetaDescription);

public sealed record TopicSummaryDto(
    int Id,
    int StoreId,
    string SystemName,
    bool IsPublished,
    string? DefaultTitle,
    DateTime UpdatedAtUtc);

public sealed record TopicDetailDto(
    int Id,
    int StoreId,
    string SystemName,
    bool IsPublished,
    DateTime? PublishedFromUtc,
    DateTime? PublishedToUtc,
    IReadOnlyList<TopicLocalizationDto> Localizations,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public sealed record TopicLocalizationRequest(
    int LanguageId,
    string Title,
    string Body,
    string? MetaTitle,
    string? MetaDescription);

public sealed record CreateTopicRequest(
    int StoreId,
    string SystemName,
    bool IsPublished,
    DateTime? PublishedFromUtc,
    DateTime? PublishedToUtc,
    IReadOnlyList<TopicLocalizationRequest> Localizations);

public sealed record UpdateTopicRequest(
    string SystemName,
    bool IsPublished,
    DateTime? PublishedFromUtc,
    DateTime? PublishedToUtc,
    IReadOnlyList<TopicLocalizationRequest> Localizations);

public sealed record WidgetZoneDto(int Id, string SystemName, string Name, string? Description, int DisplayOrder);

public sealed record WidgetInstanceDto(
    int Id,
    int StoreId,
    int WidgetZoneId,
    string ZoneSystemName,
    WidgetType WidgetType,
    string ConfigurationJson,
    int? LanguageId,
    int DisplayOrder,
    bool IsActive);

public sealed record CreateWidgetInstanceRequest(
    int StoreId,
    int WidgetZoneId,
    WidgetType WidgetType,
    string ConfigurationJson,
    int? LanguageId,
    int DisplayOrder,
    bool IsActive = true);

public sealed record UpdateWidgetInstanceRequest(
    WidgetType WidgetType,
    string ConfigurationJson,
    int? LanguageId,
    int DisplayOrder,
    bool IsActive);

public sealed record MenuItemLocalizationDto(int Id, int LanguageId, string Title);

public sealed record MenuItemDto(
    int Id,
    int? ParentMenuItemId,
    MenuItemLinkType LinkType,
    string? Url,
    int? ContentPageId,
    int? TopicId,
    string? ExternalSlug,
    int DisplayOrder,
    bool OpenInNewTab,
    IReadOnlyList<MenuItemLocalizationDto> Localizations);

public sealed record MenuSummaryDto(int Id, int StoreId, string SystemName, string Name, bool IsPublished);

public sealed record MenuDetailDto(
    int Id,
    int StoreId,
    string SystemName,
    string Name,
    bool IsPublished,
    IReadOnlyList<MenuItemDto> Items,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public sealed record MenuItemRequest(
    int? Id,
    int? ParentMenuItemId,
    MenuItemLinkType LinkType,
    string? Url,
    int? ContentPageId,
    int? TopicId,
    string? ExternalSlug,
    int DisplayOrder,
    bool OpenInNewTab,
    IReadOnlyList<MenuItemLocalizationRequest> Localizations);

public sealed record MenuItemLocalizationRequest(int LanguageId, string Title);

public sealed record CreateMenuRequest(int StoreId, string SystemName, string Name, bool IsPublished, IReadOnlyList<MenuItemRequest> Items);

public sealed record UpdateMenuRequest(string SystemName, string Name, bool IsPublished, IReadOnlyList<MenuItemRequest> Items);

public interface IContentPageAdminService
{
    Task<Result<IReadOnlyList<ContentPageSummaryDto>>> ListAsync(int? storeId, CancellationToken cancellationToken = default);
    Task<Result<ContentPageDetailDto>> GetAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<ContentPageDetailDto>> CreateAsync(CreateContentPageRequest request, CancellationToken cancellationToken = default);
    Task<Result<ContentPageDetailDto>> UpdateAsync(int id, UpdateContentPageRequest request, CancellationToken cancellationToken = default);
    Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<Result> PublishAsync(int id, CancellationToken cancellationToken = default);
    Task<Result> UnpublishAsync(int id, CancellationToken cancellationToken = default);
}

public interface ITopicAdminService
{
    Task<Result<IReadOnlyList<TopicSummaryDto>>> ListAsync(int? storeId, CancellationToken cancellationToken = default);
    Task<Result<TopicDetailDto>> GetAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<TopicDetailDto>> CreateAsync(CreateTopicRequest request, CancellationToken cancellationToken = default);
    Task<Result<TopicDetailDto>> UpdateAsync(int id, UpdateTopicRequest request, CancellationToken cancellationToken = default);
    Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default);
}

public interface IWidgetAdminService
{
    Task<Result<IReadOnlyList<WidgetZoneDto>>> ListZonesAsync(CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<WidgetInstanceDto>>> ListInstancesAsync(int? storeId, string? zoneSystemName, CancellationToken cancellationToken = default);
    Task<Result<WidgetInstanceDto>> CreateInstanceAsync(CreateWidgetInstanceRequest request, CancellationToken cancellationToken = default);
    Task<Result<WidgetInstanceDto>> UpdateInstanceAsync(int id, UpdateWidgetInstanceRequest request, CancellationToken cancellationToken = default);
    Task<Result> DeleteInstanceAsync(int id, CancellationToken cancellationToken = default);
}

public interface IMenuAdminService
{
    Task<Result<IReadOnlyList<MenuSummaryDto>>> ListAsync(int? storeId, CancellationToken cancellationToken = default);
    Task<Result<MenuDetailDto>> GetAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<MenuDetailDto>> CreateAsync(CreateMenuRequest request, CancellationToken cancellationToken = default);
    Task<Result<MenuDetailDto>> UpdateAsync(int id, UpdateMenuRequest request, CancellationToken cancellationToken = default);
    Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
