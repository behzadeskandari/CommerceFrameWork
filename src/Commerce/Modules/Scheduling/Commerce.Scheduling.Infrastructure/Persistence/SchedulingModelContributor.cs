using Commerce.Framework.Data.Db;
using Commerce.Scheduling.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Commerce.Scheduling.Infrastructure.Persistence;

public sealed class SchedulingModelContributor : ICommerceModelContributor
{
    public void ConfigureModel(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new BackgroundJobConfiguration());
        modelBuilder.ApplyConfiguration(new BackgroundJobExecutionConfiguration());
        modelBuilder.ApplyConfiguration(new RecurringJobScheduleConfiguration());
        modelBuilder.ApplyConfiguration(new JobDistributedLockConfiguration());
    }
}
