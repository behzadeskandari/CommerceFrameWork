using Commerce.Catalog.Application.Categories;
using Commerce.Catalog.Application.DependencyInjection;
using Commerce.Catalog.Application.Products;
using Commerce.Catalog.Infrastructure.DependencyInjection;
using Commerce.Catalog.Infrastructure.Persistence;
using Commerce.Catalog.Domain.Enums;
using Commerce.Framework.Data.Configuration;
using Commerce.Framework.Data.Db;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Commerce.Tests.Unit.Catalog;

internal static class CatalogTestComposition
{
    public static ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOptions<CommerceDataOptions>();
        services.AddSingleton<ICommerceModelContributor, CatalogModelContributor>();
        services.AddSingleton<ICommerceDbContextConfigurator, InMemoryCatalogDbContextConfigurator>();
        services.AddCatalogInfrastructure();
        services.AddCatalogApplication();
        services.AddCommerceDbContext();

        return services.BuildServiceProvider();
    }

    private sealed class InMemoryCatalogDbContextConfigurator : ICommerceDbContextConfigurator
    {
        private readonly string _databaseName = Guid.NewGuid().ToString("N");

        public void Configure(DbContextOptionsBuilder optionsBuilder, CommerceDataOptions dataOptions) =>
            optionsBuilder.UseInMemoryDatabase(_databaseName);
    }
}

public sealed class ProductServiceTests
{
    [Fact]
    public async Task CreateProduct_DuplicateSku_ReturnsConflict()
    {
        await using var provider = CatalogTestComposition.BuildProvider();
        using var scope = provider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IProductService>();

        Assert.True((await service.CreateAsync(new CreateProductRequest("One", "DUP-1", ProductType.Simple))).IsSuccess);
        var duplicate = await service.CreateAsync(new CreateProductRequest("Two", "dup-1", ProductType.Simple));
        Assert.False(duplicate.IsSuccess);
    }

    [Fact]
    public async Task DeleteProduct_SoftDeletesProduct()
    {
        await using var provider = CatalogTestComposition.BuildProvider();
        using var scope = provider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IProductService>();

        var created = await service.CreateAsync(new CreateProductRequest("Delete Me", "DEL-1", ProductType.Simple));
        Assert.True((await service.DeleteAsync(created.Value!.Id)).IsSuccess);

        var read = await service.GetByIdAsync(created.Value.Id);
        Assert.False(read.IsSuccess);
    }
}

public sealed class CategoryServiceTests
{
    [Fact]
    public async Task UpdateCategory_Cycle_ReturnsValidationError()
    {
        await using var provider = CatalogTestComposition.BuildProvider();
        using var scope = provider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<ICategoryService>();

        var root = await service.CreateAsync(new CreateCategoryRequest("Root"));
        var child = await service.CreateAsync(new CreateCategoryRequest("Child", root.Value!.Id));

        var cycle = await service.UpdateAsync(root.Value!.Id, new UpdateCategoryRequest(
            "Root",
            child.Value!.Id));

        Assert.False(cycle.IsSuccess);
    }

    [Fact]
    public async Task AssignProductToCategory_CreatesRelationship()
    {
        await using var provider = CatalogTestComposition.BuildProvider();
        using var scope = provider.CreateScope();
        var products = scope.ServiceProvider.GetRequiredService<IProductService>();
        var categories = scope.ServiceProvider.GetRequiredService<ICategoryService>();

        var category = await categories.CreateAsync(new CreateCategoryRequest("Games"));
        var product = await products.CreateAsync(new CreateProductRequest(
            "Game",
            "GAME-1",
            ProductType.Simple,
            CategoryIds: [category.Value!.Id]));

        Assert.Contains(category.Value.Id, product.Value!.CategoryIds);
    }
}
