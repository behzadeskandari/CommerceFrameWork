using Microsoft.EntityFrameworkCore;

namespace Commerce.Framework.Data.Db;

public interface ICommerceModelContributor
{
    void ConfigureModel(ModelBuilder modelBuilder);
}
