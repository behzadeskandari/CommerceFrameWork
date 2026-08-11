using Commerce.Framework.Contracts.Seeding;

namespace Commerce.Framework.Contracts.Seeding;

public interface IModuleSeeder : ICommerceSeeder
{
    string ModuleSystemName { get; }
}
