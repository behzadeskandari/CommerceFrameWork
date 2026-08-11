namespace Commerce.Host.Configuration;

public sealed class CorsOptions
{
    public const string SectionName = "Commerce:Cors";

    public string[] AllowedOrigins { get; init; } =
    [
        "http://localhost:4200",
        "http://localhost:4201"
    ];
}
