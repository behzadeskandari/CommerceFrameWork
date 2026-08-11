namespace Commerce.Framework.Contracts.Seeding;

public interface ICommerceSeeder
{
    int Order { get; }

    string Name { get; }

    Task SeedAsync(SeederContext context, CancellationToken cancellationToken = default);
}

public sealed class SeederContext
{
    public required IServiceProvider Services { get; init; }
}
