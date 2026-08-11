namespace Commerce.Framework.Contracts.Configuration;

public interface ICommerceSettings
{
    string ApplicationName { get; }
    string EnvironmentName { get; }
    string? BaseUrl { get; }
}
