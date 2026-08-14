using Commerce.Cms.Domain.Entities;
using Commerce.Cms.Domain.Enums;

namespace Commerce.Cms.Application.Abstractions;

public interface ICmsRepository
{
    Task<IReadOnlyList<ContentPage>> ListPagesAsync(int? storeId, CancellationToken cancellationToken = default);
    Task<ContentPage?> GetPageByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ContentPage?> GetPageBySlugAsync(int storeId, int languageId, string slug, CancellationToken cancellationToken = default);
    Task<bool> PageSlugExistsAsync(int storeId, int languageId, string slug, int? excludePageId, CancellationToken cancellationToken = default);
    Task AddPageAsync(ContentPage page, CancellationToken cancellationToken = default);
    Task SavePageAsync(ContentPage page, CancellationToken cancellationToken = default);
    Task DeletePageAsync(ContentPage page, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Topic>> ListTopicsAsync(int? storeId, CancellationToken cancellationToken = default);
    Task<Topic?> GetTopicByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Topic?> GetTopicBySystemNameAsync(int storeId, string systemName, CancellationToken cancellationToken = default);
    Task<bool> TopicSystemNameExistsAsync(int storeId, string systemName, int? excludeTopicId, CancellationToken cancellationToken = default);
    Task AddTopicAsync(Topic topic, CancellationToken cancellationToken = default);
    Task SaveTopicAsync(Topic topic, CancellationToken cancellationToken = default);
    Task DeleteTopicAsync(Topic topic, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WidgetZone>> ListWidgetZonesAsync(CancellationToken cancellationToken = default);
    Task<WidgetZone?> GetWidgetZoneByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<WidgetZone?> GetWidgetZoneBySystemNameAsync(string systemName, CancellationToken cancellationToken = default);
    Task EnsureWidgetZonesSeededAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WidgetInstance>> ListWidgetInstancesAsync(int storeId, int? widgetZoneId, CancellationToken cancellationToken = default);
    Task<WidgetInstance?> GetWidgetInstanceByIdAsync(int id, CancellationToken cancellationToken = default);
    Task AddWidgetInstanceAsync(WidgetInstance instance, CancellationToken cancellationToken = default);
    Task SaveWidgetInstanceAsync(WidgetInstance instance, CancellationToken cancellationToken = default);
    Task DeleteWidgetInstanceAsync(WidgetInstance instance, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Menu>> ListMenusAsync(int? storeId, CancellationToken cancellationToken = default);
    Task<Menu?> GetMenuByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Menu?> GetMenuBySystemNameAsync(int storeId, string systemName, CancellationToken cancellationToken = default);
    Task<bool> MenuSystemNameExistsAsync(int storeId, string systemName, int? excludeMenuId, CancellationToken cancellationToken = default);
    Task AddMenuAsync(Menu menu, CancellationToken cancellationToken = default);
    Task SaveMenuAsync(Menu menu, CancellationToken cancellationToken = default);
    Task DeleteMenuAsync(Menu menu, CancellationToken cancellationToken = default);
}
