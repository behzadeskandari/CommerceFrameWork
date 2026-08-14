using System.Text.Json;
using Commerce.Cms.Application.Abstractions;
using Commerce.Cms.Contracts.Storefront;
using Commerce.Cms.Domain.Enums;
using Commerce.Framework.Contracts.Tenancy;

namespace Commerce.Cms.Application.Storefront;

public sealed class CmsStorefrontService(ICmsRepository repository, IStoreContext storeContext) : ICmsStorefrontService
{
    public async Task<StorefrontPageDto?> GetPageBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var storeId = storeContext.CurrentStoreId;
        var languageId = storeContext.CurrentLanguageId;
        if (!storeId.HasValue || !languageId.HasValue)
        {
            return null;
        }

        var normalizedSlug = slug.Trim().ToLowerInvariant();
        var page = await repository.GetPageBySlugAsync(storeId.Value, languageId.Value, normalizedSlug, cancellationToken).ConfigureAwait(false);
        if (page is null || !page.IsVisible(DateTime.UtcNow))
        {
            return null;
        }

        var localization = page.Localizations.FirstOrDefault(x => x.LanguageId == languageId.Value)
            ?? page.Localizations.FirstOrDefault();
        if (localization is null)
        {
            return null;
        }

        return new StorefrontPageDto(
            page.Id,
            localization.Title,
            localization.Slug,
            localization.Body,
            localization.MetaTitle ?? localization.Title,
            localization.MetaDescription,
            localization.MetaKeywords,
            localization.CanonicalUrl,
            localization.LanguageId);
    }

    public async Task<StorefrontMenuDto?> GetMenuAsync(string systemName, CancellationToken cancellationToken = default)
    {
        var storeId = storeContext.CurrentStoreId;
        var languageId = storeContext.CurrentLanguageId;
        if (!storeId.HasValue || !languageId.HasValue)
        {
            return null;
        }

        var menu = await repository.GetMenuBySystemNameAsync(storeId.Value, systemName.Trim().ToLowerInvariant(), cancellationToken).ConfigureAwait(false);
        if (menu is null || !menu.IsPublished)
        {
            return null;
        }

        var items = menu.Items
            .Where(x => x.ParentMenuItemId is null)
            .OrderBy(x => x.DisplayOrder)
            .Select(x => MapMenuItem(x, menu.Items, languageId.Value))
            .ToList();

        return new StorefrontMenuDto(menu.SystemName, menu.Name, items);
    }

    public async Task<IReadOnlyList<StorefrontWidgetDto>> GetWidgetsAsync(string zoneSystemName, CancellationToken cancellationToken = default)
    {
        var storeId = storeContext.CurrentStoreId;
        var languageId = storeContext.CurrentLanguageId;
        if (!storeId.HasValue)
        {
            return [];
        }

        await repository.EnsureWidgetZonesSeededAsync(cancellationToken).ConfigureAwait(false);
        var zone = await repository.GetWidgetZoneBySystemNameAsync(zoneSystemName, cancellationToken).ConfigureAwait(false);
        if (zone is null)
        {
            return [];
        }

        var instances = await repository.ListWidgetInstancesAsync(storeId.Value, zone.Id, cancellationToken).ConfigureAwait(false);
        var results = new List<StorefrontWidgetDto>();
        foreach (var instance in instances.Where(x => x.IsActive && (!x.LanguageId.HasValue || x.LanguageId == languageId)))
        {
            var html = await RenderWidgetAsync(instance, cancellationToken).ConfigureAwait(false);
            results.Add(new StorefrontWidgetDto(instance.Id, zone.SystemName, instance.WidgetType.ToString(), html));
        }

        return results;
    }

    private async Task<string> RenderWidgetAsync(Domain.Entities.WidgetInstance instance, CancellationToken cancellationToken)
    {
        try
        {
            using var doc = JsonDocument.Parse(instance.ConfigurationJson);
            return instance.WidgetType switch
            {
                WidgetType.HtmlBlock => doc.RootElement.TryGetProperty("html", out var html) ? html.GetString() ?? string.Empty : string.Empty,
                WidgetType.TopicEmbed when doc.RootElement.TryGetProperty("systemName", out var topicName) =>
                    await RenderTopicAsync(topicName.GetString() ?? string.Empty, cancellationToken).ConfigureAwait(false),
                WidgetType.MenuEmbed when doc.RootElement.TryGetProperty("systemName", out var menuName) =>
                    await RenderMenuAsync(menuName.GetString() ?? string.Empty, cancellationToken).ConfigureAwait(false),
                _ => string.Empty
            };
        }
        catch (JsonException)
        {
            return string.Empty;
        }
    }

    private async Task<string> RenderTopicAsync(string systemName, CancellationToken cancellationToken)
    {
        var storeId = storeContext.CurrentStoreId;
        var languageId = storeContext.CurrentLanguageId;
        if (!storeId.HasValue || !languageId.HasValue)
        {
            return string.Empty;
        }

        var topic = await repository.GetTopicBySystemNameAsync(storeId.Value, systemName, cancellationToken).ConfigureAwait(false);
        if (topic is null || !topic.IsVisible(DateTime.UtcNow))
        {
            return string.Empty;
        }

        var loc = topic.Localizations.FirstOrDefault(x => x.LanguageId == languageId.Value) ?? topic.Localizations.FirstOrDefault();
        return loc?.Body ?? string.Empty;
    }

    private async Task<string> RenderMenuAsync(string systemName, CancellationToken cancellationToken)
    {
        var menu = await GetMenuAsync(systemName, cancellationToken).ConfigureAwait(false);
        if (menu is null)
        {
            return string.Empty;
        }

        var items = string.Join(string.Empty, menu.Items.Select(i => $"<a href=\"{i.Url}\">{i.Title}</a> "));
        return $"<nav class=\"cms-menu cms-menu-{menu.SystemName}\">{items}</nav>";
    }

    private StorefrontMenuItemDto MapMenuItem(
        Domain.Entities.MenuItem item,
        IReadOnlyCollection<Domain.Entities.MenuItem> allItems,
        int languageId)
    {
        var title = item.Localizations.FirstOrDefault(x => x.LanguageId == languageId)?.Title
            ?? item.Localizations.FirstOrDefault()?.Title
            ?? "Link";
        var url = ResolveMenuItemUrl(item);
        var children = allItems
            .Where(x => x.ParentMenuItemId == item.Id)
            .OrderBy(x => x.DisplayOrder)
            .Select(x => MapMenuItem(x, allItems, languageId))
            .ToList();
        return new StorefrontMenuItemDto(item.Id, title, url, item.OpenInNewTab, children);
    }

    private static string ResolveMenuItemUrl(Domain.Entities.MenuItem item) =>
        item.LinkType switch
        {
            MenuItemLinkType.Url => item.Url ?? "#",
            MenuItemLinkType.Page => item.ExternalSlug is not null ? $"/pages/{item.ExternalSlug}" : "#",
            MenuItemLinkType.Topic => "#",
            MenuItemLinkType.Category => item.ExternalSlug is not null ? $"/category/{item.ExternalSlug}" : "#",
            MenuItemLinkType.Product => item.ExternalSlug is not null ? $"/product/{item.ExternalSlug}" : "#",
            _ => "#"
        };
}
