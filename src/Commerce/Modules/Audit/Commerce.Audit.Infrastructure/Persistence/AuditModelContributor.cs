using Commerce.Audit.Infrastructure.Persistence.Configurations;
using Commerce.Framework.Data.Db;
using Microsoft.EntityFrameworkCore;

namespace Commerce.Audit.Infrastructure.Persistence;

public sealed class AuditModelContributor : ICommerceModelContributor
{
    public void ConfigureModel(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfiguration(new AuditEntryConfiguration());
}
