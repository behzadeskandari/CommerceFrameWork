using Commerce.Promotions.Application.Abstractions;
using Commerce.Promotions.Application.Admin;
using Commerce.Promotions.Application.Pricing;
using Commerce.Promotions.Application.Rules;
using Commerce.Promotions.Application.Usage;
using Commerce.Promotions.Application.Rules.Actions;
using Commerce.Promotions.Application.Rules.Conditions;
using Commerce.Promotions.Contracts.Admin;
using Commerce.Promotions.Contracts.Pricing;
using Commerce.Promotions.Contracts.Usage;
using Microsoft.Extensions.DependencyInjection;

namespace Commerce.Promotions.Application.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPromotionsApplication(this IServiceCollection services)
    {
        services.AddScoped<IPromotionConditionEvaluator, MinimumCartSubtotalConditionEvaluator>();
        services.AddScoped<IPromotionConditionEvaluator, MinimumQuantityConditionEvaluator>();
        services.AddScoped<IPromotionConditionEvaluator, CustomerGroupConditionEvaluator>();
        services.AddScoped<IPromotionConditionEvaluator, ProductInCartConditionEvaluator>();
        services.AddScoped<IPromotionConditionEvaluator, CategoryInCartConditionEvaluator>();
        services.AddScoped<IPromotionConditionEvaluator, ProductRestrictionConditionEvaluator>();
        services.AddScoped<IPromotionConditionEvaluator, CategoryRestrictionConditionEvaluator>();
        services.AddScoped<IPromotionConditionEvaluator, StoreRestrictionConditionEvaluator>();
        services.AddScoped<IPromotionConditionEvaluator, UsageLimitRemainingConditionEvaluator>();
        services.AddScoped<IPromotionConditionEvaluator, PerCustomerUsageRemainingConditionEvaluator>();

        services.AddScoped<IPromotionActionExecutor, PercentageDiscountActionExecutor>();
        services.AddScoped<IPromotionActionExecutor, FixedAmountDiscountActionExecutor>();
        services.AddScoped<IPromotionActionExecutor, BuyXGetYActionExecutor>();
        services.AddScoped<IPromotionActionExecutor, ApplyLinkedDiscountActionExecutor>();

        services.AddScoped<PromotionRuleEngine>();
        services.AddScoped<IPromotionEvaluationService, PromotionEvaluationService>();
        services.AddScoped<IPromotionAdminService, PromotionAdminService>();
        services.AddScoped<IPromotionUsageService, PromotionUsageService>();
        return services;
    }
}
