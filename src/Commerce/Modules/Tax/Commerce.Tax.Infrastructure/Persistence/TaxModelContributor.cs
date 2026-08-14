using Commerce.Framework.Data.Db;

using Commerce.Tax.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;



namespace Commerce.Tax.Infrastructure.Persistence;



public sealed class TaxModelContributor : ICommerceModelContributor

{

    public void ConfigureModel(ModelBuilder modelBuilder)

    {

        modelBuilder.ApplyConfiguration(new TaxCategoryConfiguration());

        modelBuilder.ApplyConfiguration(new TaxZoneConfiguration());

        modelBuilder.ApplyConfiguration(new TaxZoneCountryConfiguration());

        modelBuilder.ApplyConfiguration(new TaxZoneStateConfiguration());

        modelBuilder.ApplyConfiguration(new TaxZonePostalRuleConfiguration());

        modelBuilder.ApplyConfiguration(new TaxRateConfiguration());

    }

}


