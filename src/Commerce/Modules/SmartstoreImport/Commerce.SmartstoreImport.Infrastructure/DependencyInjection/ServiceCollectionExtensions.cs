using Commerce.SmartstoreImport.Application.Abstractions;
using Commerce.SmartstoreImport.Application.DependencyInjection;
using Commerce.SmartstoreImport.Contracts;
using Commerce.SmartstoreImport.Infrastructure.Import;
using Commerce.SmartstoreImport.Infrastructure.Import.Importers;
using Commerce.SmartstoreImport.Infrastructure.Migrations;
using Commerce.SmartstoreImport.Infrastructure.Parsing;
using Commerce.SmartstoreImport.Infrastructure.Persistence;
using Commerce.SmartstoreImport.Infrastructure.Reconciliation;
using Commerce.Framework.Data.Db;
using Commerce.Framework.Data.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Commerce.SmartstoreImport.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSmartstoreImportInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        _ = configuration;
        services.AddSmartstoreImportApplication();
        services.AddSingleton<ICommerceModelContributor, SmartstoreImportModelContributor>();
        services.AddSingleton<ICommerceMigration, SmartstoreImportInitialMigration>();
        services.AddSingleton<ISmartstoreSqlParser, SmartstoreSqlParser>();
        services.AddSingleton<ISmartstoreImportService, SmartstoreImportService>();
        services.AddSingleton<ISmartstoreReconciliationService, SmartstoreReconciliationService>();
        services.AddSingleton<ISmartstoreEntityImporter, SmartstoreStoreImporter>();
        services.AddSingleton<ISmartstoreEntityImporter, SmartstoreLanguageImporter>();
        services.AddSingleton<ISmartstoreEntityImporter, SmartstoreCurrencyImporter>();
        services.AddSingleton<ISmartstoreEntityImporter, SmartstoreSettingImporter>();
        services.AddSingleton<ISmartstoreEntityImporter, SmartstoreCustomerImporter>();
        services.AddSingleton<ISmartstoreEntityImporter, SmartstoreCategoryImporter>();
        services.AddSingleton<ISmartstoreEntityImporter, SmartstoreManufacturerImporter>();
        services.AddSingleton<ISmartstoreEntityImporter, SmartstoreProductImporter>();
        services.AddSingleton<ISmartstoreEntityImporter, SmartstoreProductVariantImporter>();
        services.AddSingleton<ISmartstoreEntityImporter, SmartstoreMediaImporter>();
        services.AddSingleton<ISmartstoreEntityImporter, SmartstoreDiscountImporter>();
        services.AddSingleton<ISmartstoreEntityImporter, SmartstoreProductReviewImporter>();
        services.AddSingleton<ISmartstoreEntityImporter, SmartstoreOrderImporter>();
        services.AddSingleton<ISmartstoreEntityImporter, SmartstoreTopicImporter>();
        services.AddSingleton<ISmartstoreEntityImporter, SmartstoreUrlRecordImporter>();
        services.AddSingleton<ISmartstoreEntityImporter, SmartstoreLocalizationImporter>();
        return services;
    }
}
