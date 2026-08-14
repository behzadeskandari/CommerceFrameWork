using Commerce.Themes.Contracts.Admin;

namespace Commerce.Themes.Contracts;

public interface IThemeStorefrontService
{
    Task<Storefront.ThemeRuntimeDto> GetRuntimeAsync(int storeId, bool isRtl, CancellationToken cancellationToken = default);
}
