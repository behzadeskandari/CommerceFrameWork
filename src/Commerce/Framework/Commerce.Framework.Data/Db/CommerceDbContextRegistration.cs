using Commerce.Framework.Data.Configuration;
using Commerce.Framework.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Commerce.Framework.Data.Db;

public static class CommerceDbContextRegistration
{
    public static IServiceCollection AddCommerceDbContext(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<DbContextOptions<CommerceDbContext>>(serviceProvider =>
        {
            var optionsBuilder =
                new DbContextOptionsBuilder<CommerceDbContext>();

            var dataOptions =
                serviceProvider
                    .GetRequiredService<IOptions<CommerceDataOptions>>()
                    .Value;

            var configurator =
                serviceProvider
                    .GetRequiredService<ICommerceDbContextConfigurator>();

            configurator.Configure(
                optionsBuilder,
                dataOptions);

            optionsBuilder.ReplaceService<
                IModelCacheKeyFactory,
                CommerceModelCacheKeyFactory>();

            return optionsBuilder.Options;
        });

        services.AddScoped<CommerceDbContext>(serviceProvider =>
            new CommerceDbContext(
                serviceProvider.GetRequiredService<
                    DbContextOptions<CommerceDbContext>>(),
                serviceProvider));

        return services;
    }
}
//public static class CommerceDbContextRegistration
//{
//    public static IServiceCollection AddCommerceDbContext(this IServiceCollection services)
//    {
//        ArgumentNullException.ThrowIfNull(services);

//        services.AddScoped<DbContextOptions<CommerceDbContext>>(serviceProvider =>
//        {
//            var optionsBuilder = new DbContextOptionsBuilder<CommerceDbContext>();
//            var dataOptions = serviceProvider.GetRequiredService<IOptions<CommerceDataOptions>>().Value;

//            serviceProvider
//                .GetRequiredService<ICommerceDbContextConfigurator>()
//                .Configure(optionsBuilder, dataOptions);

//            var interceptor = serviceProvider.GetService<DomainEventSaveChangesInterceptor>();
//            if (interceptor is not null)
//            {
//                optionsBuilder.AddInterceptors(interceptor);
//            }

//            optionsBuilder.ReplaceService<IModelCacheKeyFactory, CommerceModelCacheKeyFactory>();

//            return optionsBuilder.Options;
//        });

//        services.AddScoped<CommerceDbContext>(serviceProvider =>
//            new CommerceDbContext(
//                serviceProvider.GetRequiredService<DbContextOptions<CommerceDbContext>>(),
//                serviceProvider));

//        return services;
//    }
//}
