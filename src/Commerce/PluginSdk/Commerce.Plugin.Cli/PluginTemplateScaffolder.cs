namespace Commerce.Plugin.Cli;

internal sealed class PluginTemplateTokens
{
    public required string SystemName { get; init; }

    public required string PluginName { get; init; }

    public required string ProjectName { get; init; }

    public required string RootNamespace { get; init; }

    public required string AssemblyName { get; init; }

    public required string Category { get; init; }

    public required string Name { get; init; }
}

internal static class PluginTemplateLocator
{
    public static string ResolveTemplateRoot()
    {
        var env = Environment.GetEnvironmentVariable("COMMERCE_PLUGIN_TEMPLATE_PATH");
        if (!string.IsNullOrWhiteSpace(env) && Directory.Exists(env))
        {
            return env;
        }

        var assemblyDirectory = AppContext.BaseDirectory;
        var candidates = new[]
        {
            Path.GetFullPath(Path.Combine(assemblyDirectory, "..", "..", "..", "..", "Commerce.Plugin.Template", "content")),
            Path.GetFullPath(Path.Combine(assemblyDirectory, "templates", "commerce-plugin")),
            Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "templates", "commerce-plugin"))
        };

        return candidates.FirstOrDefault(Directory.Exists) ?? candidates[0];
    }
}

internal static class PluginTemplateScaffolder
{
    public static async Task ScaffoldAsync(string templateRoot, string destination, PluginTemplateTokens tokens)
    {
        foreach (var sourceFile in Directory.EnumerateFiles(templateRoot, "*.*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(templateRoot, sourceFile);
            var transformedRelativePath = ReplaceTokens(relativePath, tokens);
            var destinationPath = Path.Combine(destination, transformedRelativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);

            var extension = Path.GetExtension(sourceFile);
            if (extension is ".cs" or ".csproj" or ".json" or ".md" or ".targets")
            {
                var content = await File.ReadAllTextAsync(sourceFile).ConfigureAwait(false);
                await File.WriteAllTextAsync(destinationPath, ReplaceTokens(content, tokens)).ConfigureAwait(false);
            }
            else
            {
                File.Copy(sourceFile, destinationPath, overwrite: true);
            }
        }
    }

    private static string ReplaceTokens(string value, PluginTemplateTokens tokens) =>
        value
            .Replace("__SYSTEM_NAME__", tokens.SystemName, StringComparison.Ordinal)
            .Replace("__PLUGIN_NAME__", tokens.PluginName, StringComparison.Ordinal)
            .Replace("__PROJECT_NAME__", tokens.ProjectName, StringComparison.Ordinal)
            .Replace("__ROOT_NAMESPACE__", tokens.RootNamespace, StringComparison.Ordinal)
            .Replace("__ASSEMBLY_NAME__", tokens.AssemblyName, StringComparison.Ordinal)
            .Replace("__CATEGORY__", tokens.Category, StringComparison.Ordinal)
            .Replace("__NAME__", tokens.Name, StringComparison.Ordinal);
}
