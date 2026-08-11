using Commerce.Framework.Contracts.Installation;
using Microsoft.Extensions.Configuration;

namespace Commerce.Framework.Application.Installation;

public sealed class InstallationRequirementsEvaluator
{
    public IReadOnlyList<RequirementCheckResult> Evaluate(IConfiguration configuration, string contentRootPath)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentRootPath);

        var results = new List<RequirementCheckResult>
        {
            EvaluateRuntime(),
            EvaluateApplicationName(configuration),
            EvaluateWritableDirectory(contentRootPath)
        };

        return results;
    }

    private static RequirementCheckResult EvaluateRuntime()
    {
        var version = Environment.Version;
        var satisfied = version.Major >= 10;

        return new RequirementCheckResult(
            "Runtime",
            satisfied,
            satisfied
                ? $".NET runtime {version} is supported."
                : $".NET 10 or later is required. Current runtime: {version}.");
    }

    private static RequirementCheckResult EvaluateApplicationName(IConfiguration configuration)
    {
        var applicationName = configuration["Commerce:ApplicationName"];
        var satisfied = !string.IsNullOrWhiteSpace(applicationName);

        return new RequirementCheckResult(
            "ApplicationConfiguration",
            satisfied,
            satisfied
                ? "Commerce application configuration is present."
                : "Commerce:ApplicationName is missing from configuration.");
    }

    private static RequirementCheckResult EvaluateWritableDirectory(string contentRootPath)
    {
        var dataDirectory = Path.Combine(contentRootPath, "App_Data");
        Directory.CreateDirectory(dataDirectory);

        var probeFile = Path.Combine(dataDirectory, ".write-test");
        try
        {
            File.WriteAllText(probeFile, "ok");
            File.Delete(probeFile);

            return new RequirementCheckResult(
                "WritableAppData",
                true,
                "App_Data directory is writable.");
        }
        catch (Exception ex)
        {
            return new RequirementCheckResult(
                "WritableAppData",
                false,
                $"App_Data directory is not writable: {ex.Message}");
        }
    }
}
