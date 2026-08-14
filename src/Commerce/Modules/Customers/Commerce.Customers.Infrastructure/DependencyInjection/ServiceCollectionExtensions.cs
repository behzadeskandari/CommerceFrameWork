using Commerce.Customers.Application.Abstractions;
using Commerce.Customers.Contracts.Customers;
using Commerce.Customers.Infrastructure.Identity;
using Commerce.Customers.Infrastructure.Persistence.Repositories;
using Commerce.Customers.Infrastructure.Security;
using Commerce.Framework.Contracts.Security;
using Commerce.Framework.Data.Db;
using Commerce.Framework.Data.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace Commerce.Customers.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCustomersInfrastructure(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();

        services.AddIdentity<CommerceIdentityUser, CommerceIdentityRole>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequiredLength = 8;

                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.AllowedForNewUsers = true;

                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<CommerceDbContext>()
            .AddClaimsPrincipalFactory<CommerceUserClaimsPrincipalFactory>();

        services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            options.Cookie.SameSite = SameSiteMode.Lax;
        });

        services.AddSingleton<PermissionRegistry>();
        services.AddScoped<IPermissionService, PermissionService>();
        services.AddScoped<ICustomerRepository, EfCustomerRepository>();
        services.AddScoped<ICustomerAddressRepository, EfCustomerAddressRepository>();
        services.AddScoped<ICustomerPreferenceRepository, EfCustomerPreferenceRepository>();
        services.AddScoped<ICustomerSegmentRepository, EfCustomerSegmentRepository>();
        services.AddScoped<ILoyaltyRepository, EfLoyaltyRepository>();
        services.AddScoped<IStoreCreditRepository, EfStoreCreditRepository>();
        services.AddScoped<ICustomerActivityRepository, EfCustomerActivityRepository>();
        services.AddScoped<IAffiliateRepository, EfAffiliateRepository>();
        services.AddScoped<ICurrentCustomerContext, CurrentCustomerContext>();

        return services;
    }
}
