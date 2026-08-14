using Commerce.Cms.Application.Abstractions;
using Commerce.Cms.Contracts.Admin;
using Commerce.Cms.Domain.Entities;
using Commerce.Framework.Core.Errors;
using Commerce.Framework.Core.Results;

namespace Commerce.Cms.Application.Admin;

public sealed class MenuAdminService(ICmsRepository repository) : IMenuAdminService
{
    public async Task<Result<IReadOnlyList<MenuSummaryDto>>> ListAsync(int? storeId, CancellationToken cancellationToken = default)
    {
        var menus = await repository.ListMenusAsync(storeId, cancellationToken).ConfigureAwait(false);
        return Result.Success<IReadOnlyList<MenuSummaryDto>>(menus.Select(m => new MenuSummaryDto(m.Id, m.StoreId, m.SystemName, m.Name, m.IsPublished)).ToList());
    }

    public async Task<Result<MenuDetailDto>> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        var menu = await repository.GetMenuByIdAsync(id, cancellationToken).ConfigureAwait(false);
        return menu is null
            ? Result.Failure<MenuDetailDto>(Error.NotFound($"Menu '{id}' was not found."))
            : Result.Success(MapDetail(menu));
    }

    public async Task<Result<MenuDetailDto>> CreateAsync(CreateMenuRequest request, CancellationToken cancellationToken = default)
    {
        if (await repository.MenuSystemNameExistsAsync(request.StoreId, request.SystemName, null, cancellationToken).ConfigureAwait(false))
        {
            return Result.Failure<MenuDetailDto>(Error.Validation($"Menu system name '{request.SystemName}' already exists."));
        }

        var menu = Menu.Create(request.StoreId, request.SystemName, request.Name, request.IsPublished);
        ApplyItems(menu, request.Items);
        await repository.AddMenuAsync(menu, cancellationToken).ConfigureAwait(false);
        return Result.Success(MapDetail(menu));
    }

    public async Task<Result<MenuDetailDto>> UpdateAsync(int id, UpdateMenuRequest request, CancellationToken cancellationToken = default)
    {
        var menu = await repository.GetMenuByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (menu is null)
        {
            return Result.Failure<MenuDetailDto>(Error.NotFound($"Menu '{id}' was not found."));
        }

        if (await repository.MenuSystemNameExistsAsync(menu.StoreId, request.SystemName, menu.Id, cancellationToken).ConfigureAwait(false))
        {
            return Result.Failure<MenuDetailDto>(Error.Validation($"Menu system name '{request.SystemName}' already exists."));
        }

        menu.Update(request.SystemName, request.Name, request.IsPublished);
        ApplyItems(menu, request.Items);
        await repository.SaveMenuAsync(menu, cancellationToken).ConfigureAwait(false);
        return Result.Success(MapDetail(menu));
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var menu = await repository.GetMenuByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (menu is null)
        {
            return Result.Failure(Error.NotFound($"Menu '{id}' was not found."));
        }

        await repository.DeleteMenuAsync(menu, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    private static void ApplyItems(Menu menu, IReadOnlyList<MenuItemRequest> items)
    {
        var built = new List<MenuItem>();
        foreach (var item in items.OrderBy(x => x.DisplayOrder))
        {
            var menuItem = MenuItem.Create(
                menu.Id,
                item.ParentMenuItemId,
                item.LinkType,
                item.Url,
                item.ContentPageId,
                item.TopicId,
                item.ExternalSlug,
                item.DisplayOrder,
                item.OpenInNewTab);
            foreach (var loc in item.Localizations)
            {
                menuItem.AddLocalization(loc.LanguageId, loc.Title);
            }

            built.Add(menuItem);
        }

        menu.ReplaceItems(built);
    }

    private static MenuDetailDto MapDetail(Menu menu) =>
        new(
            menu.Id,
            menu.StoreId,
            menu.SystemName,
            menu.Name,
            menu.IsPublished,
            menu.Items.Select(item => new MenuItemDto(
                item.Id,
                item.ParentMenuItemId,
                item.LinkType,
                item.Url,
                item.ContentPageId,
                item.TopicId,
                item.ExternalSlug,
                item.DisplayOrder,
                item.OpenInNewTab,
                item.Localizations.Select(x => new MenuItemLocalizationDto(x.Id, x.LanguageId, x.Title)).ToList())).ToList(),
            menu.CreatedAtUtc,
            menu.UpdatedAtUtc);
}
