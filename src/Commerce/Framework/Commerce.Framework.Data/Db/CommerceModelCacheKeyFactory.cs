using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Commerce.Framework.Data.Db;

public sealed class CommerceModelCacheKeyFactory : IModelCacheKeyFactory
{
    public object Create(DbContext context, bool designTime)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context is not CommerceDbContext commerceContext)
        {
            return (context.GetType(), designTime);
        }

        return (context.GetType(), commerceContext.GetModelContributorKey(), designTime);
    }
}
