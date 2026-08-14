using Commerce.Framework.Data.Db;

using Commerce.Payments.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;



namespace Commerce.Payments.Infrastructure.Persistence;



public sealed class PaymentsModelContributor : ICommerceModelContributor

{

    public void ConfigureModel(ModelBuilder modelBuilder)

    {

        modelBuilder.ApplyConfiguration(new PaymentConfiguration());

        modelBuilder.ApplyConfiguration(new PaymentTransactionConfiguration());

        modelBuilder.ApplyConfiguration(new PaymentMethodConfiguration());

        modelBuilder.ApplyConfiguration(new PaymentAttemptConfiguration());

        modelBuilder.ApplyConfiguration(new RefundConfiguration());

        modelBuilder.ApplyConfiguration(new RefundTransactionConfiguration());

        modelBuilder.ApplyConfiguration(new PaymentCallbackRecordConfiguration());
        modelBuilder.ApplyConfiguration(new GiftCardConfiguration());
        modelBuilder.ApplyConfiguration(new GiftCardTransactionConfiguration());

    }

}

