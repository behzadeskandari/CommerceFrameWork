using Commerce.Cms.Application.Abstractions;
using Commerce.Cms.Domain.Entities;
using Commerce.Cms.Domain.Enums;
using Commerce.Framework.Data.Db;
using Microsoft.EntityFrameworkCore;

namespace Commerce.Cms.Infrastructure.Persistence.Repositories;

public sealed class EfCmsRepository(CommerceDbContext dbContext) : ICmsRepository
{
    public Task<IReadOnlyList<ContentPage>> ListPagesAsync(int? storeId, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Set<ContentPage>().AsNoTracking().Include(x => x.Localizations).AsQueryable();
        if (storeId.HasValue)
        {
            query = query.Where(x => x.StoreId == storeId.Value);
        }

        return query.OrderByDescending(x => x.UpdatedAtUtc).ToListAsync(cancellationToken)
            .ContinueWith(t => (IReadOnlyList<ContentPage>)t.Result, cancellationToken);
    }

    public Task<ContentPage?> GetPageByIdAsync(int id, CancellationToken cancellationToken = default) =>
        dbContext.Set<ContentPage>().Include(x => x.Localizations).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<ContentPage?> GetPageBySlugAsync(int storeId, int languageId, string slug, CancellationToken cancellationToken = default)
    {
        var localization = await dbContext.Set<ContentPageLocalization>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.LanguageId == languageId && x.Slug == slug, cancellationToken)
            .ConfigureAwait(false);
        if (localization is null)
        {
            return null;
        }

        return await dbContext.Set<ContentPage>()
            .Include(x => x.Localizations)
            .FirstOrDefaultAsync(x => x.Id == localization.ContentPageId && x.StoreId == storeId, cancellationToken)
            .ConfigureAwait(false);
    }

    public Task<bool> PageSlugExistsAsync(int storeId, int languageId, string slug, int? excludePageId, CancellationToken cancellationToken = default) =>
        dbContext.Set<ContentPageLocalization>()
            .AnyAsync(x =>
                x.LanguageId == languageId &&
                x.Slug == slug &&
                dbContext.Set<ContentPage>().Any(p => p.Id == x.ContentPageId && p.StoreId == storeId && (!excludePageId.HasValue || p.Id != excludePageId.Value)),
                cancellationToken);

    public async Task AddPageAsync(ContentPage page, CancellationToken cancellationToken = default)
    {
        dbContext.Set<ContentPage>().Add(page);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task SavePageAsync(ContentPage page, CancellationToken cancellationToken = default)
    {
        var existing = await dbContext.Set<ContentPage>()
            .Include(x => x.Localizations)
            .FirstOrDefaultAsync(x => x.Id == page.Id, cancellationToken)
            .ConfigureAwait(false);
        if (existing is null)
        {
            return;
        }

        dbContext.Entry(existing).CurrentValues.SetValues(page);
        dbContext.Set<ContentPageLocalization>().RemoveRange(existing.Localizations);
        dbContext.Set<ContentPageLocalization>().AddRange(page.Localizations);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task DeletePageAsync(ContentPage page, CancellationToken cancellationToken = default)
    {
        dbContext.Set<ContentPage>().Remove(page);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public Task<IReadOnlyList<Topic>> ListTopicsAsync(int? storeId, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Set<Topic>().AsNoTracking().Include(x => x.Localizations).AsQueryable();
        if (storeId.HasValue)
        {
            query = query.Where(x => x.StoreId == storeId.Value);
        }

        return query.OrderByDescending(x => x.UpdatedAtUtc).ToListAsync(cancellationToken)
            .ContinueWith(t => (IReadOnlyList<Topic>)t.Result, cancellationToken);
    }

    public Task<Topic?> GetTopicByIdAsync(int id, CancellationToken cancellationToken = default) =>
        dbContext.Set<Topic>().Include(x => x.Localizations).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<Topic?> GetTopicBySystemNameAsync(int storeId, string systemName, CancellationToken cancellationToken = default) =>
        dbContext.Set<Topic>().Include(x => x.Localizations)
            .FirstOrDefaultAsync(x => x.StoreId == storeId && x.SystemName == systemName, cancellationToken);

    public Task<bool> TopicSystemNameExistsAsync(int storeId, string systemName, int? excludeTopicId, CancellationToken cancellationToken = default) =>
        dbContext.Set<Topic>().AnyAsync(x => x.StoreId == storeId && x.SystemName == systemName && (!excludeTopicId.HasValue || x.Id != excludeTopicId.Value), cancellationToken);

    public async Task AddTopicAsync(Topic topic, CancellationToken cancellationToken = default)
    {
        dbContext.Set<Topic>().Add(topic);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task SaveTopicAsync(Topic topic, CancellationToken cancellationToken = default)
    {
        var existing = await dbContext.Set<Topic>().Include(x => x.Localizations).FirstOrDefaultAsync(x => x.Id == topic.Id, cancellationToken).ConfigureAwait(false);
        if (existing is null)
        {
            return;
        }

        dbContext.Entry(existing).CurrentValues.SetValues(topic);
        dbContext.Set<TopicLocalization>().RemoveRange(existing.Localizations);
        dbContext.Set<TopicLocalization>().AddRange(topic.Localizations);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteTopicAsync(Topic topic, CancellationToken cancellationToken = default)
    {
        dbContext.Set<Topic>().Remove(topic);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public Task<IReadOnlyList<WidgetZone>> ListWidgetZonesAsync(CancellationToken cancellationToken = default) =>
        dbContext.Set<WidgetZone>().AsNoTracking().OrderBy(x => x.DisplayOrder).ToListAsync(cancellationToken)
            .ContinueWith(t => (IReadOnlyList<WidgetZone>)t.Result, cancellationToken);

    public Task<WidgetZone?> GetWidgetZoneByIdAsync(int id, CancellationToken cancellationToken = default) =>
        dbContext.Set<WidgetZone>().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<WidgetZone?> GetWidgetZoneBySystemNameAsync(string systemName, CancellationToken cancellationToken = default) =>
        dbContext.Set<WidgetZone>().FirstOrDefaultAsync(x => x.SystemName == systemName, cancellationToken);

    public async Task EnsureWidgetZonesSeededAsync(CancellationToken cancellationToken = default)
    {
        var existing = await dbContext.Set<WidgetZone>().Select(x => x.SystemName).ToListAsync(cancellationToken).ConfigureAwait(false);
        var order = 0;
        foreach (var name in WidgetZoneNames.All)
        {
            if (existing.Contains(name, StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            dbContext.Set<WidgetZone>().Add(WidgetZone.Create(name, name, null, order++));
        }

        if (dbContext.ChangeTracker.HasChanges())
        {
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public Task<IReadOnlyList<WidgetInstance>> ListWidgetInstancesAsync(int storeId, int? widgetZoneId, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Set<WidgetInstance>().AsNoTracking().Where(x => x.StoreId == storeId);
        if (widgetZoneId.HasValue)
        {
            query = query.Where(x => x.WidgetZoneId == widgetZoneId.Value);
        }

        return query.OrderBy(x => x.DisplayOrder).ToListAsync(cancellationToken)
            .ContinueWith(t => (IReadOnlyList<WidgetInstance>)t.Result, cancellationToken);
    }

    public Task<WidgetInstance?> GetWidgetInstanceByIdAsync(int id, CancellationToken cancellationToken = default) =>
        dbContext.Set<WidgetInstance>().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task AddWidgetInstanceAsync(WidgetInstance instance, CancellationToken cancellationToken = default)
    {
        dbContext.Set<WidgetInstance>().Add(instance);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task SaveWidgetInstanceAsync(WidgetInstance instance, CancellationToken cancellationToken = default)
    {
        dbContext.Set<WidgetInstance>().Update(instance);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteWidgetInstanceAsync(WidgetInstance instance, CancellationToken cancellationToken = default)
    {
        dbContext.Set<WidgetInstance>().Remove(instance);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public Task<IReadOnlyList<Menu>> ListMenusAsync(int? storeId, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Set<Menu>().AsNoTracking()
            .Include(x => x.Items).ThenInclude(i => i.Localizations)
            .AsQueryable();
        if (storeId.HasValue)
        {
            query = query.Where(x => x.StoreId == storeId.Value);
        }

        return query.OrderBy(x => x.Name).ToListAsync(cancellationToken)
            .ContinueWith(t => (IReadOnlyList<Menu>)t.Result, cancellationToken);
    }

    public Task<Menu?> GetMenuByIdAsync(int id, CancellationToken cancellationToken = default) =>
        dbContext.Set<Menu>().Include(x => x.Items).ThenInclude(i => i.Localizations).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<Menu?> GetMenuBySystemNameAsync(int storeId, string systemName, CancellationToken cancellationToken = default) =>
        dbContext.Set<Menu>().Include(x => x.Items).ThenInclude(i => i.Localizations)
            .FirstOrDefaultAsync(x => x.StoreId == storeId && x.SystemName == systemName, cancellationToken);

    public Task<bool> MenuSystemNameExistsAsync(int storeId, string systemName, int? excludeMenuId, CancellationToken cancellationToken = default) =>
        dbContext.Set<Menu>().AnyAsync(x => x.StoreId == storeId && x.SystemName == systemName && (!excludeMenuId.HasValue || x.Id != excludeMenuId.Value), cancellationToken);

    public async Task AddMenuAsync(Menu menu, CancellationToken cancellationToken = default)
    {
        dbContext.Set<Menu>().Add(menu);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task SaveMenuAsync(Menu menu, CancellationToken cancellationToken = default)
    {
        var existing = await dbContext.Set<Menu>().Include(x => x.Items).ThenInclude(i => i.Localizations)
            .FirstOrDefaultAsync(x => x.Id == menu.Id, cancellationToken).ConfigureAwait(false);
        if (existing is null)
        {
            return;
        }

        dbContext.Entry(existing).CurrentValues.SetValues(menu);
        foreach (var item in existing.Items.ToList())
        {
            dbContext.Set<MenuItemLocalization>().RemoveRange(item.Localizations);
            dbContext.Set<MenuItem>().Remove(item);
        }

        dbContext.Set<MenuItem>().AddRange(menu.Items);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteMenuAsync(Menu menu, CancellationToken cancellationToken = default)
    {
        dbContext.Set<Menu>().Remove(menu);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
