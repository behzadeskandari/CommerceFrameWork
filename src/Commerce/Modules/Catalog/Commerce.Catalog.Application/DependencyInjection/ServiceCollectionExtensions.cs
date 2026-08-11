using Commerce.Catalog.Application.Categories;
using Commerce.Catalog.Application.Products;
using Commerce.Catalog.Contracts.Catalog;
using Commerce.Catalog.Contracts.Categories;
using Commerce.Catalog.Contracts.Products;
using Microsoft.Extensions.DependencyInjection;

namespace Commerce.Catalog.Application.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCatalogApplication(this IServiceCollection services)
    {
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IProductReader>(sp => sp.GetRequiredService<IProductService>());
        services.AddScoped<ICategoryReader>(sp => sp.GetRequiredService<ICategoryService>());
        services.AddScoped<IProductCatalog, ProductCatalog>();

        return services;
    }
}

internal sealed class ProductCatalog(IProductService productService, ICategoryService categoryService) : IProductCatalog
{
    public async Task<ProductDetailDto?> GetProductAsync(int productId, CancellationToken cancellationToken = default)
    {
        var result = await productService.GetByIdAsync(productId, cancellationToken).ConfigureAwait(false);
        return result.IsSuccess ? result.Value : null;
    }

    public async Task<IReadOnlyList<ProductSummaryDto>> GetProductsAsync(CancellationToken cancellationToken = default)
    {
        var result = await productService.ListAsync(includeDeleted: false, cancellationToken).ConfigureAwait(false);
        return result.IsSuccess ? result.Value! : Array.Empty<ProductSummaryDto>();
    }

    public async Task<CategoryDetailDto?> GetCategoryAsync(int categoryId, CancellationToken cancellationToken = default)
    {
        var result = await categoryService.GetByIdAsync(categoryId, cancellationToken).ConfigureAwait(false);
        return result.IsSuccess ? result.Value : null;
    }

    public async Task<IReadOnlyList<CategorySummaryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default)
    {
        var result = await categoryService.ListAsync(cancellationToken).ConfigureAwait(false);
        return result.IsSuccess ? result.Value! : Array.Empty<CategorySummaryDto>();
    }
}
