using System.CommandLine;
using System.Diagnostics;
using Commerce.Plugin.Contracts;
using Commerce.Plugin.Sdk;

namespace Commerce.Plugin.Cli;

internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        var pluginCommand = new Command("plugin", "Create, build, test, pack, and validate Commerce plugins.");

        pluginCommand.AddCommand(CreateCreateCommand());
        pluginCommand.AddCommand(CreateBuildCommand());
        pluginCommand.AddCommand(CreateTestCommand());
        pluginCommand.AddCommand(CreatePackCommand());
        pluginCommand.AddCommand(CreateValidateCommand());

        var root = new RootCommand("Commerce developer CLI")
        {
            pluginCommand
        };

        return await root.InvokeAsync(args).ConfigureAwait(false);
    }

    private static Command CreateCreateCommand()
    {
        var categoryOption = new Option<string>("--category", "Plugin category segment, e.g. Payment or Sample.")
        {
            IsRequired = true
        };
        var nameOption = new Option<string>("--name", "Plugin name segment, e.g. Manual or HelloWorld.")
        {
            IsRequired = true
        };
        var outputOption = new Option<string>("--output", () => Directory.GetCurrentDirectory(), "Output directory.");
        var command = new Command("create", "Scaffold a new plugin from the Commerce template.");

        command.AddOption(categoryOption);
        command.AddOption(nameOption);
        command.AddOption(outputOption);
        command.SetHandler(async (category, name, output) =>
        {
            var systemName = $"{category}.{name}";
            var projectName = $"Commerce.Plugin.{category}.{name}";
            var destination = Path.Combine(output, projectName);
            if (Directory.Exists(destination))
            {
                Console.Error.WriteLine($"Destination already exists: {destination}");
                Environment.ExitCode = 1;
                return;
            }

            var templateRoot = PluginTemplateLocator.ResolveTemplateRoot();
            if (!Directory.Exists(templateRoot))
            {
                Console.Error.WriteLine($"Template directory not found: {templateRoot}");
                Environment.ExitCode = 1;
                return;
            }

            Directory.CreateDirectory(destination);
            await PluginTemplateScaffolder.ScaffoldAsync(templateRoot, destination, new PluginTemplateTokens
            {
                SystemName = systemName,
                PluginName = $"{name} {category} Plugin",
                ProjectName = projectName,
                RootNamespace = projectName,
                AssemblyName = $"{projectName}.dll",
                Category = category,
                Name = name
            }).ConfigureAwait(false);

            Console.WriteLine($"Created plugin project at {destination}");
            Console.WriteLine($"Next: cd {destination} && dotnet build");
        }, categoryOption, nameOption, outputOption);

        return command;
    }

    private static Command CreateBuildCommand()
    {
        var projectOption = new Option<FileInfo?>("--project", "Path to the plugin .csproj file.");
        var command = new Command("build", "Build a plugin project.");

        command.AddOption(projectOption);
        command.SetHandler(async project =>
        {
            var projectPath = ResolveProjectPath(project);
            var exitCode = await RunDotNetAsync("build", projectPath, "--nologo").ConfigureAwait(false);
            Environment.ExitCode = exitCode;
        }, projectOption);

        return command;
    }

    private static Command CreateTestCommand()
    {
        var projectOption = new Option<FileInfo?>("--project", "Path to the plugin or test .csproj file.");
        var command = new Command("test", "Run plugin unit tests.");

        command.AddOption(projectOption);
        command.SetHandler(async project =>
        {
            var projectPath = ResolveProjectPath(project);
            var exitCode = await RunDotNetAsync("test", projectPath, "--nologo").ConfigureAwait(false);
            Environment.ExitCode = exitCode;
        }, projectOption);

        return command;
    }

    private static Command CreatePackCommand()
    {
        var directoryOption = new Option<string?>("--directory", "Plugin build output directory.");
        var outputOption = new Option<string?>("--output", "Output zip path.");
        var projectOption = new Option<FileInfo?>("--project", "Plugin project used to locate the build output.");
        var command = new Command("pack", "Create a distributable plugin package (.zip).");

        command.AddOption(directoryOption);
        command.AddOption(outputOption);
        command.AddOption(projectOption);
        command.SetHandler((directory, output, project) =>
        {
            var projectPath = ResolveProjectPath(project);
            var pluginDirectory = directory ?? Path.Combine(Path.GetDirectoryName(projectPath)!, "bin", "Debug", "net10.0");
            if (!Directory.Exists(pluginDirectory))
            {
                Console.Error.WriteLine($"Plugin output directory not found: {pluginDirectory}. Build the project first.");
                Environment.ExitCode = 1;
                return;
            }

            var manifestPath = Path.Combine(pluginDirectory, PluginPackageLayout.ManifestFileName);
            if (!File.Exists(manifestPath))
            {
                Console.Error.WriteLine("Plugin.json was not found in the build output.");
                Environment.ExitCode = 1;
                return;
            }

            var report = PluginSdkValidator.ValidateDirectory(pluginDirectory);
            if (!report.IsValid)
            {
                Console.Error.WriteLine(string.Join(Environment.NewLine, report.Errors));
                Environment.ExitCode = 1;
                return;
            }

            var zipPath = output ?? Path.Combine(pluginDirectory, $"{report.Manifest!.SystemName}.zip");
            PluginPackagePacker.PackDirectory(pluginDirectory, zipPath);
            Console.WriteLine($"Created package: {zipPath}");
        }, directoryOption, outputOption, projectOption);

        return command;
    }

    private static Command CreateValidateCommand()
    {
        var directoryOption = new Option<string?>("--directory", "Plugin directory to validate.");
        var projectOption = new Option<FileInfo?>("--project", "Plugin project file to validate.");
        var commerceVersionOption = new Option<string>("--commerce-version", () => "1.0.0", "Target Commerce version.");
        var command = new Command("validate", "Validate plugin manifest, references, and package layout.");

        command.AddOption(directoryOption);
        command.AddOption(projectOption);
        command.AddOption(commerceVersionOption);
        command.SetHandler((directory, project, commerceVersionValue) =>
        {
            var projectPath = project is null ? FindProjectPath(Directory.GetCurrentDirectory()) : project.FullName;
            if (projectPath is null)
            {
                Console.Error.WriteLine("Could not locate a plugin .csproj file.");
                Environment.ExitCode = 1;
                return;
            }

            var commerceVersion = Version.Parse(commerceVersionValue.Split('-', '+')[0]);
            var projectReport = PluginProjectValidator.ValidateProject(projectPath);
            var pluginDirectory = directory ?? Path.Combine(Path.GetDirectoryName(projectPath)!, "bin", "Debug", "net10.0");
            var outputReport = Directory.Exists(pluginDirectory)
                ? PluginSdkValidator.ValidateDirectory(pluginDirectory, new PluginValidationOptions { CommerceVersion = commerceVersion })
                : new PluginValidationReport();

            if (!Directory.Exists(pluginDirectory))
            {
                projectReport.Warnings.Add($"Build output not found at '{pluginDirectory}'. Build the plugin to validate the assembly.");
            }

            foreach (var error in projectReport.Errors.Concat(outputReport.Errors))
            {
                Console.Error.WriteLine(error);
            }

            foreach (var warning in projectReport.Warnings.Concat(outputReport.Warnings))
            {
                Console.WriteLine($"warning: {warning}");
            }

            if (projectReport.Errors.Count > 0 || outputReport.Errors.Count > 0)
            {
                Environment.ExitCode = 1;
                return;
            }

            Console.WriteLine("Validation succeeded.");
        }, directoryOption, projectOption, commerceVersionOption);

        return command;
    }

    private static string ResolveProjectPath(FileInfo? project)
    {
        if (project is not null)
        {
            return project.FullName;
        }

        return FindProjectPath(Directory.GetCurrentDirectory())
            ?? throw new InvalidOperationException("Could not locate a plugin .csproj file in the current directory.");
    }

    private static string? FindProjectPath(string directory)
    {
        var current = new DirectoryInfo(directory);
        while (current is not null)
        {
            var project = current.GetFiles("Commerce.Plugin.*.csproj").FirstOrDefault()
                ?? current.GetFiles("*.csproj").FirstOrDefault();
            if (project is not null)
            {
                return project.FullName;
            }

            current = current.Parent;
        }

        return null;
    }

    private static async Task<int> RunDotNetAsync(string verb, string projectPath, params string[] args)
    {
        var arguments = new List<string> { verb, projectPath };
        arguments.AddRange(args);

        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                ArgumentList = { },
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            }
        };

        foreach (var argument in arguments)
        {
            process.StartInfo.ArgumentList.Add(argument);
        }

        process.Start();
        var stdout = await process.StandardOutput.ReadToEndAsync().ConfigureAwait(false);
        var stderr = await process.StandardError.ReadToEndAsync().ConfigureAwait(false);
        await process.WaitForExitAsync().ConfigureAwait(false);

        if (!string.IsNullOrWhiteSpace(stdout))
        {
            Console.WriteLine(stdout);
        }

        if (!string.IsNullOrWhiteSpace(stderr))
        {
            Console.Error.WriteLine(stderr);
        }

        return process.ExitCode;
    }
}
