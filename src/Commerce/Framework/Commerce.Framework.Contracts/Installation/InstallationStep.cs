namespace Commerce.Framework.Contracts.Installation;

public enum InstallationStep
{
    Requirements = 1,
    Database = 2,
    Migrate = 3,
    Seed = 4,
    Administrator = 5,
    Store = 6,
    Language = 7,
    Currency = 8,
    Complete = 9
}
