using Commerce.Framework.Core.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Commerce.Framework.Events.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCommerceEvents(this IServiceCollection services)
    {
        services.AddScoped<DomainEventSaveChangesInterceptor>();
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
        services.AddScoped<IEventBus, InProcessEventBus>();
        return services;
    }

    public static DbContextOptionsBuilder AddDomainEventDispatch(this DbContextOptionsBuilder optionsBuilder, IServiceProvider serviceProvider)
    {
        var interceptor = serviceProvider.GetRequiredService<DomainEventSaveChangesInterceptor>();
        return optionsBuilder.AddInterceptors(interceptor);
    }
}
