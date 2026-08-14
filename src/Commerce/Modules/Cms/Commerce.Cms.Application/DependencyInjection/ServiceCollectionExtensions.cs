using Commerce.Cms.Application.Abstractions;
using Commerce.Cms.Application.Admin;
using Commerce.Cms.Application.Security;
using Commerce.Cms.Application.Storefront;
using Commerce.Cms.Contracts.Admin;
using Commerce.Cms.Contracts.Storefront;
using Microsoft.Extensions.DependencyInjection;

namespace Commerce.Cms.Application.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCmsApplication(this IServiceCollection services)
    {
        services.AddScoped<IContentHtmlSanitizer, ContentHtmlSanitizer>();
        services.AddScoped<IContentPageAdminService, ContentPageAdminService>();
        services.AddScoped<ITopicAdminService, TopicAdminService>();
        services.AddScoped<IWidgetAdminService, WidgetAdminService>();
        services.AddScoped<IMenuAdminService, MenuAdminService>();
        services.AddScoped<ICmsStorefrontService, CmsStorefrontService>();
        return services;
    }
}
