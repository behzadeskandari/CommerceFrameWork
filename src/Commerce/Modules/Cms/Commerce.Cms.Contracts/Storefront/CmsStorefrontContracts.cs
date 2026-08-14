namespace Commerce.Cms.Contracts.Storefront;

public sealed record StorefrontPageDto(
    int Id,
    string Title,
    string Slug,
    string Body,
    string? MetaTitle,
    string? MetaDescription,
    string? MetaKeywords,
    string? CanonicalUrl,
    int LanguageId);

public sealed record StorefrontMenuItemDto(
    int Id,
    string Title,
    string Url,
    bool OpenInNewTab,
    IReadOnlyList<StorefrontMenuItemDto> Children);

public sealed record StorefrontMenuDto(string SystemName, string Name, IReadOnlyList<StorefrontMenuItemDto> Items);

public sealed record StorefrontWidgetDto(
    int Id,
    string ZoneSystemName,
    string WidgetType,
    string RenderedHtml);

public interface ICmsStorefrontService
{
    Task<StorefrontPageDto?> GetPageBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<StorefrontMenuDto?> GetMenuAsync(string systemName, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StorefrontWidgetDto>> GetWidgetsAsync(string zoneSystemName, CancellationToken cancellationToken = default);
}
