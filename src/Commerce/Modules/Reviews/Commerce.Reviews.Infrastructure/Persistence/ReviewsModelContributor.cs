using Commerce.Framework.Data.Db;
using Commerce.Reviews.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Commerce.Reviews.Infrastructure.Persistence;

public sealed class ReviewsModelContributor : ICommerceModelContributor
{
    public void ConfigureModel(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new ProductReviewConfiguration());
        modelBuilder.ApplyConfiguration(new WishlistConfiguration());
        modelBuilder.ApplyConfiguration(new WishlistItemConfiguration());
    }
}
