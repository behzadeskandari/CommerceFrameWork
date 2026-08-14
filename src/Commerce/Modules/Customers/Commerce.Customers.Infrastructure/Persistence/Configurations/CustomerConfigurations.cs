using Commerce.Customers.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Commerce.Customers.Infrastructure.Persistence.Configurations;

internal sealed class CustomerCustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("CustomerCustomer");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.IdentityUserId).HasMaxLength(450).IsRequired();
        builder.Property(x => x.Email).HasMaxLength(Customer.EmailMaxLength).IsRequired();
        builder.Property(x => x.NormalizedEmail).HasMaxLength(Customer.EmailMaxLength).IsRequired();
        builder.Property(x => x.FirstName).HasMaxLength(Customer.NameMaxLength).IsRequired();
        builder.Property(x => x.LastName).HasMaxLength(Customer.NameMaxLength).IsRequired();
        builder.Property(x => x.PhoneNumber).HasMaxLength(Customer.PhoneMaxLength);
        builder.Property(x => x.TaxRegistrationNumber).HasMaxLength(100);
        builder.Property(x => x.IsTaxExempt).HasDefaultValue(false);
        builder.Property(x => x.CustomerGroupId);
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.UpdatedAtUtc).IsRequired();
        builder.HasIndex(x => x.IdentityUserId).IsUnique();
        builder.HasIndex(x => x.NormalizedEmail).IsUnique().HasFilter("[Deleted] = 0");
        builder.HasIndex(x => x.Active);
        builder.HasIndex(x => x.Deleted);
    }
}

internal sealed class CustomerAddressConfiguration : IEntityTypeConfiguration<CustomerAddress>
{
    public void Configure(EntityTypeBuilder<CustomerAddress> builder)
    {
        builder.ToTable("CustomerAddress");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Label).HasMaxLength(CustomerAddress.LabelMaxLength).IsRequired();
        builder.Property(x => x.FirstName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.LastName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Country).HasMaxLength(200).IsRequired();
        builder.Property(x => x.StateProvince).HasMaxLength(200);
        builder.Property(x => x.City).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Address1).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Address2).HasMaxLength(500);
        builder.Property(x => x.PostalCode).HasMaxLength(50).IsRequired();
        builder.Property(x => x.PhoneNumber).HasMaxLength(50);
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.UpdatedAtUtc).IsRequired();
        builder.HasIndex(x => x.CustomerId);
        builder.HasOne<Customer>()
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
