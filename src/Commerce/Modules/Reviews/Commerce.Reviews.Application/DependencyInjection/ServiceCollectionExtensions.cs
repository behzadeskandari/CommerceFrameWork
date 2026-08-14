using Commerce.Reviews.Application.Abstractions;
using Commerce.Reviews.Application.Admin;
using Commerce.Reviews.Application.Storefront;
using Commerce.Reviews.Contracts.Admin;
using Commerce.Reviews.Contracts.Storefront;
using Microsoft.Extensions.DependencyInjection;

namespace Commerce.Reviews.Application.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddReviewsApplication(this IServiceCollection services)
    {
        services.AddScoped<IReviewStorefrontService, ReviewStorefrontService>();
        services.AddScoped<IWishlistStorefrontService, WishlistStorefrontService>();
        services.AddScoped<IReviewAdminService, ReviewAdminService>();
        services.AddScoped<IWishlistAdminService, WishlistAdminService>();
        return services;
    }
}
