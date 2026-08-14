using Commerce.DisasterRecovery.Infrastructure.Persistence.Configurations;
using Commerce.Framework.Data.Db;
using Microsoft.EntityFrameworkCore;

namespace Commerce.DisasterRecovery.Infrastructure.Persistence;

public sealed class DisasterRecoveryModelContributor : ICommerceModelContributor
{
    public void ConfigureModel(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new BackupRunConfiguration());
        modelBuilder.ApplyConfiguration(new BackupArtifactConfiguration());
        modelBuilder.ApplyConfiguration(new RecoveryTestRunConfiguration());
    }
}
