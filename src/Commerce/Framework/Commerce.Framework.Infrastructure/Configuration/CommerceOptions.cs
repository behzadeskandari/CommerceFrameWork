namespace Commerce.Framework.Infrastructure.Configuration;

public sealed class CommerceOptions
{
    public const string SectionName = "Commerce";

    public string ApplicationName { get; set; } = "Commerce";

    public string Environment { get; set; } = "Production";

    public string? BaseUrl { get; set; }
}
