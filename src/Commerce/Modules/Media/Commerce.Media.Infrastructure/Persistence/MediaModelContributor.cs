using Commerce.Framework.Data.Db;
using Commerce.Media.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Commerce.Media.Infrastructure.Persistence;

public sealed class MediaModelContributor : ICommerceModelContributor
{
    public void ConfigureModel(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfiguration(new MediaAssetConfiguration());
}
