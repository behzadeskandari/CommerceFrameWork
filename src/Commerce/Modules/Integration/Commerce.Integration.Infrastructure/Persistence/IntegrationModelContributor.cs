using Commerce.Framework.Data.Db;
using Commerce.Integration.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Commerce.Integration.Infrastructure.Persistence;

public sealed class IntegrationModelContributor : ICommerceModelContributor
{
    public void ConfigureModel(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new WebhookSubscriptionConfiguration());
        modelBuilder.ApplyConfiguration(new WebhookDeliveryConfiguration());
        modelBuilder.ApplyConfiguration(new ApiClientConfiguration());
        modelBuilder.ApplyConfiguration(new ProcessedIntegrationEventConfiguration());
    }
}
