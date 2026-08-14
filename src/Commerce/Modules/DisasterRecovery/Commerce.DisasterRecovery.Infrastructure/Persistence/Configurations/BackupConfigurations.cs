using Commerce.DisasterRecovery.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Commerce.DisasterRecovery.Infrastructure.Persistence.Configurations;

internal sealed class BackupRunConfiguration : IEntityTypeConfiguration<BackupRun>
{
    public void Configure(EntityTypeBuilder<BackupRun> builder)
    {
        builder.ToTable("DisasterRecoveryBackupRuns");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.BackupKey).HasMaxLength(64).IsRequired();
        builder.Property(x => x.RootPath).HasMaxLength(512).IsRequired();
        builder.Property(x => x.ManifestRelativePath).HasMaxLength(256);
        builder.Property(x => x.FailureMessage).HasMaxLength(2000);
        builder.Property(x => x.IntegritySnapshotJson).HasMaxLength(8000).IsRequired();
        builder.Property(x => x.Status).HasConversion<int>().IsRequired();
        builder.Property(x => x.ValidityStatus).HasConversion<int>().IsRequired();
        builder.HasIndex(x => x.BackupKey).IsUnique();
        builder.HasIndex(x => x.StartedAtUtc);
        builder.HasMany(x => x.Artifacts).WithOne(x => x.BackupRun!).HasForeignKey(x => x.BackupRunId);
        builder.HasMany(x => x.RecoveryTests).WithOne(x => x.BackupRun!).HasForeignKey(x => x.BackupRunId);
    }
}

internal sealed class BackupArtifactConfiguration : IEntityTypeConfiguration<BackupArtifact>
{
    public void Configure(EntityTypeBuilder<BackupArtifact> builder)
    {
        builder.ToTable("DisasterRecoveryBackupArtifacts");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.RelativePath).HasMaxLength(512).IsRequired();
        builder.Property(x => x.Sha256).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Message).HasMaxLength(1000);
        builder.Property(x => x.ComponentType).HasConversion<int>().IsRequired();
    }
}

internal sealed class RecoveryTestRunConfiguration : IEntityTypeConfiguration<RecoveryTestRun>
{
    public void Configure(EntityTypeBuilder<RecoveryTestRun> builder)
    {
        builder.ToTable("DisasterRecoveryRecoveryTests");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.Message).HasMaxLength(2000);
        builder.Property(x => x.Status).HasConversion<int>().IsRequired();
        builder.HasIndex(x => new { x.BackupRunId, x.StartedAtUtc });
    }
}
