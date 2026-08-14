using Commerce.Framework.Data.Db;
using Commerce.SmartstoreImport.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Commerce.SmartstoreImport.Infrastructure.Persistence;

public sealed class SmartstoreImportModelContributor : ICommerceModelContributor
{
    public void ConfigureModel(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new ImportRunConfiguration());
        modelBuilder.ApplyConfiguration(new ImportIdMappingConfiguration());
        modelBuilder.ApplyConfiguration(new ImportIssueConfiguration());
    }
}
