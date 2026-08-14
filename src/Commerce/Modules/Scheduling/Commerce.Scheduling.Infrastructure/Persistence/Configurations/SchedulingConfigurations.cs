using Commerce.Scheduling.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Commerce.Scheduling.Infrastructure.Persistence.Configurations;

public sealed class BackgroundJobConfiguration : IEntityTypeConfiguration<BackgroundJob>
{
    public void Configure(EntityTypeBuilder<BackgroundJob> builder)
    {
        builder.ToTable("BackgroundJob");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.JobType).HasMaxLength(BackgroundJob.JobTypeMaxLength).IsRequired();
        builder.Property(x => x.PayloadJson).HasMaxLength(BackgroundJob.PayloadMaxLength);
        builder.Property(x => x.LastError).HasMaxLength(BackgroundJob.ErrorMaxLength);
        builder.Property(x => x.IdempotencyKey).HasMaxLength(BackgroundJob.IdempotencyKeyMaxLength);
        builder.Property(x => x.LockOwnerId).HasMaxLength(BackgroundJob.LockOwnerMaxLength);
        builder.Property(x => x.RecurringScheduleKey).HasMaxLength(BackgroundJob.ScheduleKeyMaxLength);
        builder.HasIndex(x => x.IdempotencyKey).IsUnique().HasFilter("[IdempotencyKey] IS NOT NULL");
        builder.HasIndex(x => new { x.Status, x.ExecuteAtUtc, x.Priority });
        builder.HasIndex(x => x.JobType);
        builder.HasIndex(x => x.CreatedAtUtc);
    }
}

public sealed class BackgroundJobExecutionConfiguration : IEntityTypeConfiguration<BackgroundJobExecution>
{
    public void Configure(EntityTypeBuilder<BackgroundJobExecution> builder)
    {
        builder.ToTable("BackgroundJobExecution");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ErrorMessage).HasMaxLength(BackgroundJobExecution.ErrorMaxLength);
        builder.HasIndex(x => x.JobId);
    }
}

public sealed class RecurringJobScheduleConfiguration : IEntityTypeConfiguration<RecurringJobSchedule>
{
    public void Configure(EntityTypeBuilder<RecurringJobSchedule> builder)
    {
        builder.ToTable("RecurringJobSchedule");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ScheduleKey).HasMaxLength(RecurringJobSchedule.ScheduleKeyMaxLength).IsRequired();
        builder.Property(x => x.JobType).HasMaxLength(RecurringJobSchedule.JobTypeMaxLength).IsRequired();
        builder.Property(x => x.PayloadJson).HasMaxLength(RecurringJobSchedule.PayloadMaxLength);
        builder.HasIndex(x => x.ScheduleKey).IsUnique();
        builder.HasIndex(x => new { x.IsEnabled, x.NextRunAtUtc });
    }
}

public sealed class JobDistributedLockConfiguration : IEntityTypeConfiguration<JobDistributedLock>
{
    public void Configure(EntityTypeBuilder<JobDistributedLock> builder)
    {
        builder.ToTable("JobDistributedLock");
        builder.HasKey(x => x.LockKey);
        builder.Property(x => x.LockKey).HasMaxLength(JobDistributedLock.LockKeyMaxLength);
        builder.Property(x => x.OwnerId).HasMaxLength(JobDistributedLock.OwnerIdMaxLength).IsRequired();
        builder.HasIndex(x => x.ExpiresAtUtc);
    }
}
