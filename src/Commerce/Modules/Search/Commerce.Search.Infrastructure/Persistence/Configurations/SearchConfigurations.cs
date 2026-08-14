using Commerce.Search.Domain.Entities;
using Commerce.Framework.Data.Db;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Commerce.Search.Infrastructure.Persistence.Configurations;

public sealed class SearchIndexEntryConfiguration : IEntityTypeConfiguration<SearchIndexEntry>
{
    public void Configure(EntityTypeBuilder<SearchIndexEntry> builder)
    {
        builder.ToTable("SearchIndexEntries");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.ProductId, x.StoreId, x.LanguageId }).IsUnique();
        builder.HasIndex(x => new { x.StoreId, x.LanguageId, x.Published, x.IsDeleted });
        builder.HasIndex(x => x.Sku);
        builder.HasIndex(x => x.Name);
        builder.Property(x => x.Name).HasMaxLength(400).IsRequired();
        builder.Property(x => x.Sku).HasMaxLength(200).IsRequired();
        builder.Property(x => x.SearchText).HasMaxLength(SearchIndexEntry.TextMaxLength).IsRequired();
        builder.Property(x => x.CategoryIdsJson).HasMaxLength(SearchIndexEntry.JsonMaxLength).IsRequired();
        builder.Property(x => x.CategoryNamesJson).HasMaxLength(SearchIndexEntry.JsonMaxLength).IsRequired();
        builder.Property(x => x.TagsJson).HasMaxLength(SearchIndexEntry.JsonMaxLength).IsRequired();
        builder.Property(x => x.AttributesJson).HasMaxLength(SearchIndexEntry.JsonMaxLength).IsRequired();
    }
}

public sealed class SearchIndexJobConfiguration : IEntityTypeConfiguration<SearchIndexJob>
{
    public void Configure(EntityTypeBuilder<SearchIndexJob> builder)
    {
        builder.ToTable("SearchIndexJobs");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.Status, x.CreatedAtUtc });
        builder.Property(x => x.ErrorMessage).HasMaxLength(2000);
    }
}
