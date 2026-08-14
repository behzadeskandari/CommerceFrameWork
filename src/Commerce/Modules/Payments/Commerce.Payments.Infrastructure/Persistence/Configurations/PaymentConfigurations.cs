using Commerce.Payments.Domain.Entities;

using Microsoft.EntityFrameworkCore;

using Microsoft.EntityFrameworkCore.Metadata.Builders;



namespace Commerce.Payments.Infrastructure.Persistence.Configurations;



internal sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>

{

    public void Configure(EntityTypeBuilder<Payment> builder)

    {

        builder.ToTable("Payment");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Currency).HasMaxLength(Payment.CurrencyMaxLength).IsRequired();

        builder.Property(x => x.Amount).HasPrecision(18, 4);

        builder.Property(x => x.RefundedAmount).HasPrecision(18, 4);

        builder.Property(x => x.ProviderSystemName).HasMaxLength(Payment.ProviderSystemNameMaxLength).IsRequired();

        builder.Property(x => x.ProviderPaymentId).HasMaxLength(Payment.ProviderPaymentIdMaxLength);

        builder.Property(x => x.Metadata).HasMaxLength(Payment.MetadataMaxLength);

        builder.Property(x => x.IdempotencyKey).HasMaxLength(Payment.IdempotencyKeyMaxLength);

        builder.HasIndex(x => x.StoreId);

        builder.HasIndex(x => x.OrderId).IsUnique();

        builder.HasIndex(x => new { x.StoreId, x.IdempotencyKey }).IsUnique().HasFilter("[IdempotencyKey] IS NOT NULL");



        builder.HasMany(x => x.Transactions).WithOne().HasForeignKey(x => x.PaymentId).OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.Transactions).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(x => x.Attempts).WithOne().HasForeignKey(x => x.PaymentId).OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.Attempts).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(x => x.Refunds).WithOne().HasForeignKey(x => x.PaymentId).OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.Refunds).UsePropertyAccessMode(PropertyAccessMode.Field);

    }

}



internal sealed class PaymentTransactionConfiguration : IEntityTypeConfiguration<PaymentTransaction>

{

    public void Configure(EntityTypeBuilder<PaymentTransaction> builder)

    {

        builder.ToTable("PaymentTransaction");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Amount).HasPrecision(18, 4);

        builder.Property(x => x.Currency).HasMaxLength(PaymentTransaction.CurrencyMaxLength).IsRequired();

        builder.Property(x => x.ProviderTransactionId).HasMaxLength(PaymentTransaction.ProviderTransactionIdMaxLength);

        builder.Property(x => x.RequestReference).HasMaxLength(PaymentTransaction.ReferenceMaxLength);

        builder.Property(x => x.ResponseReference).HasMaxLength(PaymentTransaction.ReferenceMaxLength);

        builder.Property(x => x.FailureCode).HasMaxLength(PaymentTransaction.FailureCodeMaxLength);

        builder.Property(x => x.FailureMessage).HasMaxLength(PaymentTransaction.FailureMessageMaxLength);

        builder.HasIndex(x => x.PaymentId);

    }

}



internal sealed class PaymentMethodConfiguration : IEntityTypeConfiguration<PaymentMethod>

{

    public void Configure(EntityTypeBuilder<PaymentMethod> builder)

    {

        builder.ToTable("PaymentMethod");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).HasMaxLength(PaymentMethod.NameMaxLength).IsRequired();

        builder.Property(x => x.SystemName).HasMaxLength(PaymentMethod.SystemNameMaxLength).IsRequired();

        builder.Property(x => x.ProviderSystemName).HasMaxLength(PaymentMethod.ProviderSystemNameMaxLength).IsRequired();

        builder.Property(x => x.DisplayName).HasMaxLength(PaymentMethod.DisplayNameMaxLength).IsRequired();

        builder.Property(x => x.ConfigurationJson).HasMaxLength(PaymentMethod.ConfigurationJsonMaxLength);

        builder.HasIndex(x => new { x.StoreId, x.SystemName }).IsUnique();

        builder.HasIndex(x => x.StoreId);

    }

}



internal sealed class PaymentAttemptConfiguration : IEntityTypeConfiguration<PaymentAttempt>

{

    public void Configure(EntityTypeBuilder<PaymentAttempt> builder)

    {

        builder.ToTable("PaymentAttempt");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.FailureMessage).HasMaxLength(PaymentAttempt.FailureMessageMaxLength);

        builder.HasIndex(x => x.PaymentId);

    }

}



internal sealed class RefundConfiguration : IEntityTypeConfiguration<Refund>

{

    public void Configure(EntityTypeBuilder<Refund> builder)

    {

        builder.ToTable("Refund");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Amount).HasPrecision(18, 4);

        builder.Property(x => x.Currency).HasMaxLength(Refund.CurrencyMaxLength).IsRequired();

        builder.Property(x => x.Reason).HasMaxLength(Refund.ReasonMaxLength);
        builder.Property(x => x.IdempotencyKey).HasMaxLength(Refund.IdempotencyKeyMaxLength);

        builder.HasIndex(x => x.PaymentId);
        builder.HasIndex(x => new { x.PaymentId, x.IdempotencyKey })
            .IsUnique()
            .HasFilter("[IdempotencyKey] IS NOT NULL");



        builder.HasMany(x => x.Transactions).WithOne().HasForeignKey(x => x.RefundId).OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.Transactions).UsePropertyAccessMode(PropertyAccessMode.Field);

    }

}



internal sealed class RefundTransactionConfiguration : IEntityTypeConfiguration<RefundTransaction>

{

    public void Configure(EntityTypeBuilder<RefundTransaction> builder)

    {

        builder.ToTable("RefundTransaction");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Amount).HasPrecision(18, 4);

        builder.Property(x => x.ProviderTransactionId).HasMaxLength(RefundTransaction.ProviderTransactionIdMaxLength);

        builder.HasIndex(x => x.RefundId);

    }

}



internal sealed class PaymentCallbackRecordConfiguration : IEntityTypeConfiguration<PaymentCallbackRecord>

{

    public void Configure(EntityTypeBuilder<PaymentCallbackRecord> builder)

    {

        builder.ToTable("PaymentCallbackRecord");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ProviderSystemName).HasMaxLength(PaymentCallbackRecord.ProviderSystemNameMaxLength).IsRequired();

        builder.Property(x => x.CallbackKey).HasMaxLength(PaymentCallbackRecord.CallbackKeyMaxLength).IsRequired();

        builder.Property(x => x.PayloadHash).HasMaxLength(PaymentCallbackRecord.PayloadHashMaxLength).IsRequired();

        builder.HasIndex(x => new { x.ProviderSystemName, x.CallbackKey }).IsUnique();

    }

}

