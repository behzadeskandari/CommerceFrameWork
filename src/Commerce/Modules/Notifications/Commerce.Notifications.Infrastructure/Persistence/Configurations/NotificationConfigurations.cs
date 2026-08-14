using Commerce.Notifications.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Commerce.Notifications.Infrastructure.Persistence.Configurations;

public sealed class NotificationTemplateConfiguration : IEntityTypeConfiguration<NotificationTemplate>
{
    public void Configure(EntityTypeBuilder<NotificationTemplate> builder)
    {
        builder.ToTable("NotificationTemplate");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.SystemName).HasMaxLength(NotificationTemplate.SystemNameMaxLength).IsRequired();
        builder.Property(x => x.Subject).HasMaxLength(NotificationTemplate.SubjectMaxLength).IsRequired();
        builder.Property(x => x.Body).HasMaxLength(NotificationTemplate.BodyMaxLength).IsRequired();
        builder.Property(x => x.VariablesJson).HasMaxLength(NotificationTemplate.VariablesMaxLength);
        builder.HasIndex(x => x.SystemName).IsUnique();
        builder.HasIndex(x => new { x.EventType, x.Channel, x.StoreId, x.LanguageId });
    }
}

public sealed class NotificationLogConfiguration : IEntityTypeConfiguration<NotificationLog>
{
    public void Configure(EntityTypeBuilder<NotificationLog> builder)
    {
        builder.ToTable("NotificationLog");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Recipient).HasMaxLength(NotificationLog.RecipientMaxLength).IsRequired();
        builder.Property(x => x.Subject).HasMaxLength(NotificationLog.SubjectMaxLength).IsRequired();
        builder.Property(x => x.Body).HasMaxLength(16000);
        builder.Property(x => x.LastError).HasMaxLength(NotificationLog.ErrorMaxLength);
        builder.HasIndex(x => new { x.Status, x.NextRetryAtUtc });
        builder.HasIndex(x => x.CustomerId);
        builder.HasIndex(x => x.CreatedAtUtc);
    }
}

public sealed class InAppNotificationConfiguration : IEntityTypeConfiguration<InAppNotification>
{
    public void Configure(EntityTypeBuilder<InAppNotification> builder)
    {
        builder.ToTable("InAppNotification");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).HasMaxLength(InAppNotification.TitleMaxLength).IsRequired();
        builder.Property(x => x.Body).HasMaxLength(InAppNotification.BodyMaxLength).IsRequired();
        builder.HasIndex(x => new { x.CustomerId, x.IsRead });
    }
}
