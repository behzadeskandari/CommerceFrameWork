using Commerce.Framework.Contracts.Configuration;
using Commerce.Framework.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace Commerce.Framework.Infrastructure.Configuration;

internal sealed class CommerceSettings : ICommerceSettings
{
    private readonly CommerceOptions _options;

    public CommerceSettings(IOptions<CommerceOptions> options)
    {
        _options = options.Value;
    }

    public string ApplicationName => _options.ApplicationName;

    public string EnvironmentName => _options.Environment;

    public string? BaseUrl => _options.BaseUrl;
}
