using Commerce.Integration.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Commerce.Integration.Infrastructure.Persistence.Configurations;

internal sealed class WebhookSubscriptionConfiguration : IEntityTypeConfiguration<WebhookSubscription>
{
    public void Configure(EntityTypeBuilder<WebhookSubscription> builder)
    {
        builder.ToTable("WebhookSubscription");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(WebhookSubscription.NameMaxLength).IsRequired();
        builder.Property(x => x.Url).HasMaxLength(WebhookSubscription.UrlMaxLength).IsRequired();
        builder.Property(x => x.Secret).HasMaxLength(WebhookSubscription.SecretMaxLength).IsRequired();
        builder.Property(x => x.EventTypesCsv).HasMaxLength(WebhookSubscription.EventTypesMaxLength).IsRequired();
        builder.HasIndex(x => x.StoreId);
    }
}

internal sealed class WebhookDeliveryConfiguration : IEntityTypeConfiguration<WebhookDelivery>
{
    public void Configure(EntityTypeBuilder<WebhookDelivery> builder)
    {
        builder.ToTable("WebhookDelivery");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.EventType).HasMaxLength(128).IsRequired();
        builder.Property(x => x.PayloadJson).HasMaxLength(WebhookDelivery.PayloadMaxLength).IsRequired();
        builder.Property(x => x.IdempotencyKey).HasMaxLength(WebhookDelivery.IdempotencyKeyMaxLength).IsRequired();
        builder.Property(x => x.ErrorMessage).HasMaxLength(WebhookDelivery.ErrorMaxLength);
        builder.HasIndex(x => new { x.WebhookSubscriptionId, x.IdempotencyKey }).IsUnique();
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.NextRetryAtUtc);
    }
}

internal sealed class ApiClientConfiguration : IEntityTypeConfiguration<ApiClient>
{
    public void Configure(EntityTypeBuilder<ApiClient> builder)
    {
        builder.ToTable("ApiClient");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(ApiClient.NameMaxLength).IsRequired();
        builder.Property(x => x.KeyPrefix).HasMaxLength(ApiClient.KeyPrefixMaxLength).IsRequired();
        builder.Property(x => x.KeyHash).HasMaxLength(ApiClient.KeyHashMaxLength).IsRequired();
        builder.Property(x => x.ScopesCsv).HasMaxLength(ApiClient.ScopesMaxLength).IsRequired();
        builder.HasIndex(x => x.KeyPrefix).IsUnique();
        builder.HasIndex(x => x.StoreId);
    }
}

internal sealed class ProcessedIntegrationEventConfiguration : IEntityTypeConfiguration<ProcessedIntegrationEvent>
{
    public void Configure(EntityTypeBuilder<ProcessedIntegrationEvent> builder)
    {
        builder.ToTable("ProcessedIntegrationEvent");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.EventType).HasMaxLength(ProcessedIntegrationEvent.EventTypeMaxLength).IsRequired();
        builder.Property(x => x.ConsumerKey).HasMaxLength(ProcessedIntegrationEvent.ConsumerKeyMaxLength).IsRequired();
        builder.HasIndex(x => new { x.IntegrationEventId, x.ConsumerKey }).IsUnique();
    }
}
