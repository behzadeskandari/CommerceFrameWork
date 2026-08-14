using Commerce.Framework.Infrastructure.Configuration;

namespace Commerce.Tests.Unit.Deployment;

public sealed class CommerceDeploymentOptionsTests
{
    [Fact]
    public void SectionName_IsUnderCommerceDeployment()
    {
        Assert.Equal("Commerce:Deployment", CommerceDeploymentOptions.SectionName);
    }

    [Fact]
    public void Defaults_DoNotAutoMigrate()
    {
        var options = new CommerceDeploymentOptions();

        Assert.False(options.ApplyMigrationsOnStartup);
        Assert.Equal(60, options.WaitForDatabaseSeconds);
        Assert.Equal(3, options.DatabaseRetryDelaySeconds);
    }
}

public sealed class DeploymentEnvTemplateTests
{
    [Fact]
    public void EnvExample_DocumentsRequiredSecretsWithoutRealValues()
    {
        var repoRoot = FindRepoRoot();
        var examplePath = Path.Combine(repoRoot, "deploy", "docker", ".env.example");
        Assert.True(File.Exists(examplePath), $"Missing {examplePath}");

        var content = File.ReadAllText(examplePath);

        Assert.Contains("MSSQL_SA_PASSWORD", content, StringComparison.Ordinal);
        Assert.Contains("never commit", content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ProductionSecret", content, StringComparison.Ordinal);
    }

    [Fact]
    public void DockerCompose_Dev_ReferencesSqlServerAndRedis()
    {
        var repoRoot = FindRepoRoot();
        var composePath = Path.Combine(repoRoot, "deploy", "docker", "docker-compose.yml");
        var dockerfilePath = Path.Combine(repoRoot, "deploy", "docker", "Dockerfile");
        var compose = File.ReadAllText(composePath);
        var dockerfile = File.ReadAllText(dockerfilePath);

        Assert.Contains("mcr.microsoft.com/mssql/server", compose, StringComparison.Ordinal);
        Assert.Contains("redis:7-alpine", compose, StringComparison.Ordinal);
        Assert.Contains("restart:", compose, StringComparison.Ordinal);
        Assert.Contains("/health/live", dockerfile, StringComparison.Ordinal);
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Commerce.sln")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root.");
    }
}
