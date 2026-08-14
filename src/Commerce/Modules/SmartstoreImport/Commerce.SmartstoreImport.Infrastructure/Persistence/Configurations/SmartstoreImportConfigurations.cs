using Commerce.SmartstoreImport.Domain.Entities;
using Commerce.SmartstoreImport.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Commerce.SmartstoreImport.Infrastructure.Persistence.Configurations;

internal sealed class ImportRunConfiguration : IEntityTypeConfiguration<ImportRun>
{
    public void Configure(EntityTypeBuilder<ImportRun> builder)
    {
        builder.ToTable("SmartstoreImportRun");
        builder.Property(x => x.SourceFilePath).HasMaxLength(1024).IsRequired();
        builder.Property(x => x.SourceFileHash).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Summary).HasMaxLength(4000);
        builder.HasIndex(x => x.SourceFileHash);
        builder.HasIndex(x => x.StartedAtUtc);
    }
}

internal sealed class ImportIdMappingConfiguration : IEntityTypeConfiguration<ImportIdMapping>
{
    public void Configure(EntityTypeBuilder<ImportIdMapping> builder)
    {
        builder.ToTable("SmartstoreImportIdMapping");
        builder.Property(x => x.EntityType).HasMaxLength(128).IsRequired();
        builder.Property(x => x.SourceKey).HasMaxLength(512);
        builder.HasIndex(x => new { x.ImportRunId, x.EntityType, x.SourceId }).IsUnique();
        builder.HasIndex(x => new { x.EntityType, x.SourceId });
    }
}

internal sealed class ImportIssueConfiguration : IEntityTypeConfiguration<ImportIssue>
{
    public void Configure(EntityTypeBuilder<ImportIssue> builder)
    {
        builder.ToTable("SmartstoreImportIssue");
        builder.Property(x => x.EntityType).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Code).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Message).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.Details).HasMaxLength(4000);
        builder.HasIndex(x => new { x.ImportRunId, x.Severity });
    }
}
