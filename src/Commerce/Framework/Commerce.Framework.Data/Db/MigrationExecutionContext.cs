using Commerce.Framework.Data.Db;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Commerce.Framework.Data.Migrations;

public sealed class MigrationExecutionContext
{
    public MigrationExecutionContext(CommerceDbContext dbContext, IServiceProvider serviceProvider)
    {
        DbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        Database = dbContext.Database;
    }

    public CommerceDbContext DbContext { get; }

    public IServiceProvider ServiceProvider { get; }

    public DatabaseFacade Database { get; }
}
