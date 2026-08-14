using Commerce.Cms.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Commerce.Cms.Infrastructure.Persistence.Configurations;

internal sealed class ContentPageConfiguration : IEntityTypeConfiguration<ContentPage>
{
    public void Configure(EntityTypeBuilder<ContentPage> builder)
    {
        builder.ToTable("CmsContentPage");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.SystemName).HasMaxLength(ContentPage.SystemNameMaxLength);
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.UpdatedAtUtc).IsRequired();
        builder.HasIndex(x => x.StoreId);
        builder.HasMany(x => x.Localizations)
            .WithOne()
            .HasForeignKey(x => x.ContentPageId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Localizations).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class ContentPageLocalizationConfiguration : IEntityTypeConfiguration<ContentPageLocalization>
{
    public void Configure(EntityTypeBuilder<ContentPageLocalization> builder)
    {
        builder.ToTable("CmsContentPageLocalization");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).HasMaxLength(ContentPageLocalization.TitleMaxLength).IsRequired();
        builder.Property(x => x.Slug).HasMaxLength(ContentPageLocalization.SlugMaxLength).IsRequired();
        builder.Property(x => x.Body).IsRequired();
        builder.Property(x => x.MetaTitle).HasMaxLength(ContentPageLocalization.MetaTitleMaxLength);
        builder.Property(x => x.MetaDescription).HasMaxLength(ContentPageLocalization.MetaDescriptionMaxLength);
        builder.Property(x => x.MetaKeywords).HasMaxLength(ContentPageLocalization.MetaKeywordsMaxLength);
        builder.Property(x => x.CanonicalUrl).HasMaxLength(ContentPageLocalization.CanonicalUrlMaxLength);
        builder.HasIndex(x => new { x.ContentPageId, x.LanguageId }).IsUnique();
        builder.HasIndex(x => new { x.LanguageId, x.Slug });
    }
}

internal sealed class TopicConfiguration : IEntityTypeConfiguration<Topic>
{
    public void Configure(EntityTypeBuilder<Topic> builder)
    {
        builder.ToTable("CmsTopic");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.SystemName).HasMaxLength(Topic.SystemNameMaxLength).IsRequired();
        builder.HasIndex(x => new { x.StoreId, x.SystemName }).IsUnique();
        builder.HasMany(x => x.Localizations)
            .WithOne()
            .HasForeignKey(x => x.TopicId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Localizations).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class TopicLocalizationConfiguration : IEntityTypeConfiguration<TopicLocalization>
{
    public void Configure(EntityTypeBuilder<TopicLocalization> builder)
    {
        builder.ToTable("CmsTopicLocalization");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).HasMaxLength(TopicLocalization.TitleMaxLength).IsRequired();
        builder.Property(x => x.Body).IsRequired();
        builder.HasIndex(x => new { x.TopicId, x.LanguageId }).IsUnique();
    }
}

internal sealed class WidgetZoneConfiguration : IEntityTypeConfiguration<WidgetZone>
{
    public void Configure(EntityTypeBuilder<WidgetZone> builder)
    {
        builder.ToTable("CmsWidgetZone");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.SystemName).HasMaxLength(WidgetZone.SystemNameMaxLength).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(WidgetZone.NameMaxLength).IsRequired();
        builder.HasIndex(x => x.SystemName).IsUnique();
    }
}

internal sealed class WidgetInstanceConfiguration : IEntityTypeConfiguration<WidgetInstance>
{
    public void Configure(EntityTypeBuilder<WidgetInstance> builder)
    {
        builder.ToTable("CmsWidgetInstance");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ConfigurationJson).HasMaxLength(WidgetInstance.ConfigurationMaxLength).IsRequired();
        builder.Property(x => x.WidgetType).IsRequired();
        builder.HasIndex(x => new { x.StoreId, x.WidgetZoneId, x.DisplayOrder });
    }
}

internal sealed class MenuConfiguration : IEntityTypeConfiguration<Menu>
{
    public void Configure(EntityTypeBuilder<Menu> builder)
    {
        builder.ToTable("CmsMenu");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.SystemName).HasMaxLength(Menu.SystemNameMaxLength).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(Menu.NameMaxLength).IsRequired();
        builder.HasIndex(x => new { x.StoreId, x.SystemName }).IsUnique();
        builder.HasMany(x => x.Items)
            .WithOne()
            .HasForeignKey(x => x.MenuId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Items).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class MenuItemConfiguration : IEntityTypeConfiguration<MenuItem>
{
    public void Configure(EntityTypeBuilder<MenuItem> builder)
    {
        builder.ToTable("CmsMenuItem");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Url).HasMaxLength(MenuItem.UrlMaxLength);
        builder.Property(x => x.ExternalSlug).HasMaxLength(MenuItem.ExternalSlugMaxLength);
        builder.Property(x => x.LinkType).IsRequired();
        builder.HasMany(x => x.Localizations)
            .WithOne()
            .HasForeignKey(x => x.MenuItemId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Localizations).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class MenuItemLocalizationConfiguration : IEntityTypeConfiguration<MenuItemLocalization>
{
    public void Configure(EntityTypeBuilder<MenuItemLocalization> builder)
    {
        builder.ToTable("CmsMenuItemLocalization");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).HasMaxLength(MenuItemLocalization.TitleMaxLength).IsRequired();
        builder.HasIndex(x => new { x.MenuItemId, x.LanguageId }).IsUnique();
    }
}
