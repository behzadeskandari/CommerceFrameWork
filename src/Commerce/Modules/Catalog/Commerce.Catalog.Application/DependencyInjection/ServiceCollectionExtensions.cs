using Commerce.Catalog.Application.Attributes;
using Commerce.Catalog.Application.Categories;
using Commerce.Catalog.Application.Media;
using Commerce.Catalog.Application.Offers;
using Commerce.Catalog.Application.Pricing;
using Commerce.Catalog.Application.Products;
using Commerce.Catalog.Application.Storefront;
using Commerce.Catalog.Application.Variants;
using Commerce.Catalog.Contracts.Attributes;
using Commerce.Catalog.Contracts.Catalog;
using Commerce.Catalog.Contracts.Categories;
using Commerce.Catalog.Contracts.Media;
using Commerce.Catalog.Contracts.Offers;
using Commerce.Catalog.Contracts.Pricing;
using Commerce.Catalog.Contracts.Products;
using Commerce.Catalog.Contracts.Variants;
using Microsoft.Extensions.DependencyInjection;

namespace Commerce.Catalog.Application.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCatalogApplication(this IServiceCollection services)
    {
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IAttributeService, AttributeService>();
        services.AddScoped<IVariantService, VariantService>();
        services.AddScoped<IOfferService, OfferService>();
        services.AddScoped<OfferTierPriceService>();
        services.AddScoped<IOfferTierPriceReader>(sp => sp.GetRequiredService<OfferTierPriceService>());
        services.AddScoped<IOfferTierPriceAdminService>(sp => sp.GetRequiredService<OfferTierPriceService>());
        services.AddScoped<PricingService>();
        services.AddScoped<IStorefrontCatalogService, StorefrontCatalogService>();
        services.AddScoped<IProductMediaService, ProductMediaService>();
        services.AddScoped<IProductMediaReader>(sp => sp.GetRequiredService<IProductMediaService>());

        services.AddScoped<IProductReader>(sp => sp.GetRequiredService<IProductService>());
        services.AddScoped<ICategoryReader>(sp => sp.GetRequiredService<ICategoryService>());
        services.AddScoped<IProductAttributeReader>(sp => sp.GetRequiredService<IAttributeService>());
        services.AddScoped<IProductVariantReader>(sp => sp.GetRequiredService<IVariantService>());
        services.AddScoped<IProductOfferReader>(sp => sp.GetRequiredService<IOfferService>());
        services.AddScoped<IPricingService>(sp => sp.GetRequiredService<PricingService>());
        services.AddScoped<ICatalogPricingReader>(sp => sp.GetRequiredService<PricingService>());
        services.AddScoped<IProductCatalog, ProductCatalog>();

        return services;
    }
}

internal sealed class ProductCatalog(
    IProductService productService,
    ICategoryService categoryService) : IProductCatalog
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
