using Commerce.Framework.Data.Configuration;
using Commerce.Framework.Data.Entities;
using Commerce.Framework.Data.Identity;
using Commerce.Framework.Data.Migrations;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.Extensions.DependencyInjection;

namespace Commerce.Framework.Data.Db;

public sealed class CommerceDbContext : IdentityDbContext<CommerceIdentityUser, CommerceIdentityRole, string>
{
    private readonly IServiceProvider _serviceProvider;

    public CommerceDbContext(
        DbContextOptions<CommerceDbContext> options,
        IServiceProvider serviceProvider)
        : base(options)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    internal string GetModelContributorKey() =>
        string.Join(
            '|',
            _serviceProvider.GetServices<ICommerceModelContributor>()
                .Select(contributor => contributor.GetType().AssemblyQualifiedName)
                .OrderBy(key => key, StringComparer.Ordinal));

    public DbSet<MigrationVersionInfo> MigrationVersionInfo => Set<MigrationVersionInfo>();

    public DbSet<CommerceInstallation> CommerceInstallations => Set<CommerceInstallation>();

    public DbSet<Setting> Settings => Set<Setting>();

    public DbSet<BootstrapStore> BootstrapStores => Set<BootstrapStore>();

    public DbSet<BootstrapLanguage> BootstrapLanguages => Set<BootstrapLanguage>();

    public DbSet<BootstrapCurrency> BootstrapCurrencies => Set<BootstrapCurrency>();

    public DbSet<BootstrapAdministrator> BootstrapAdministrators => Set<BootstrapAdministrator>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new MigrationVersionInfoConfiguration());
        modelBuilder.ApplyConfiguration(new CommerceInstallationConfiguration());
        modelBuilder.ApplyConfiguration(new SettingConfiguration());
        modelBuilder.ApplyConfiguration(new BootstrapStoreConfiguration());
        modelBuilder.ApplyConfiguration(new BootstrapLanguageConfiguration());
        modelBuilder.ApplyConfiguration(new BootstrapCurrencyConfiguration());
        modelBuilder.ApplyConfiguration(new BootstrapAdministratorConfiguration());

        foreach (var contributor in _serviceProvider.GetServices<ICommerceModelContributor>())
        {
            contributor.ConfigureModel(modelBuilder);
        }
    }

    private sealed class MigrationVersionInfoConfiguration : IEntityTypeConfiguration<MigrationVersionInfo>
    {
        public void Configure(EntityTypeBuilder<MigrationVersionInfo> builder)
        {
            builder.ToTable("MigrationVersionInfo");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Version).HasMaxLength(50).IsRequired();
            builder.Property(x => x.MigrationName).HasMaxLength(200).IsRequired();
            builder.Property(x => x.Module).HasMaxLength(100).IsRequired();
            builder.Property(x => x.AppliedAt).IsRequired();
            builder.HasIndex(x => new { x.Module, x.Version }).IsUnique();
            builder.HasIndex(x => x.MigrationName).IsUnique();
        }
    }

    private sealed class CommerceInstallationConfiguration : IEntityTypeConfiguration<CommerceInstallation>
    {
        public void Configure(EntityTypeBuilder<CommerceInstallation> builder)
        {
            builder.ToTable("CommerceInstallation");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Status).HasMaxLength(50).IsRequired();
            builder.Property(x => x.ApplicationVersion).HasMaxLength(50);
            builder.Property(x => x.InstalledVersion).HasMaxLength(50);
            builder.Property(x => x.LastError).HasMaxLength(2000);
        }
    }

    private sealed class SettingConfiguration : IEntityTypeConfiguration<Setting>
    {
        public void Configure(EntityTypeBuilder<Setting> builder)
        {
            builder.ToTable("Setting");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Name).HasMaxLength(400).IsRequired();
            builder.Property(x => x.Value).IsRequired();
            builder.Property(x => x.DataType).HasMaxLength(50).IsRequired();
            builder.HasIndex(x => new { x.Name, x.StoreId }).IsUnique();
        }
    }

    private sealed class BootstrapStoreConfiguration : IEntityTypeConfiguration<BootstrapStore>
    {
        public void Configure(EntityTypeBuilder<BootstrapStore> builder)
        {
            builder.ToTable("BootstrapStore");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Name).HasMaxLength(400).IsRequired();
            builder.Property(x => x.Url).HasMaxLength(1000).IsRequired();
            builder.Property(x => x.Hosts).HasMaxLength(1000);
        }
    }

    private sealed class BootstrapLanguageConfiguration : IEntityTypeConfiguration<BootstrapLanguage>
    {
        public void Configure(EntityTypeBuilder<BootstrapLanguage> builder)
        {
            builder.ToTable("BootstrapLanguage");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
            builder.Property(x => x.Culture).HasMaxLength(20).IsRequired();
            builder.HasIndex(x => x.Culture).IsUnique();
        }
    }

    private sealed class BootstrapCurrencyConfiguration : IEntityTypeConfiguration<BootstrapCurrency>
    {
        public void Configure(EntityTypeBuilder<BootstrapCurrency> builder)
        {
            builder.ToTable("BootstrapCurrency");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
            builder.Property(x => x.CurrencyCode).HasMaxLength(5).IsRequired();
            builder.Property(x => x.Rate).HasPrecision(18, 4);
            builder.HasIndex(x => x.CurrencyCode).IsUnique();
        }
    }

    private sealed class BootstrapAdministratorConfiguration : IEntityTypeConfiguration<BootstrapAdministrator>
    {
        public void Configure(EntityTypeBuilder<BootstrapAdministrator> builder)
        {
            builder.ToTable("BootstrapAdministrator");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Email).HasMaxLength(500).IsRequired();
            builder.Property(x => x.Username).HasMaxLength(500).IsRequired();
            builder.Property(x => x.PasswordHash).HasMaxLength(1000).IsRequired();
            builder.HasIndex(x => x.Email).IsUnique();
            builder.HasIndex(x => x.Username).IsUnique();
        }
    }
}
