# Commerce Plugin Development Guide

This guide covers building, testing, validating, and packaging plugins for the Commerce Framework using the Phase 41 Plugin SDK.

## Prerequisites

- .NET 10 SDK
- Commerce solution (monorepo) or referenced `Commerce.Framework.PluginContracts` assemblies
- Optional: `commerce` CLI from `Commerce.Plugin.Cli`

## Quick start

### 1. Create a plugin

```bash
dotnet run --project src/Commerce/PluginSdk/Commerce.Plugin.Cli -- \
  plugin create --category Sample --name Hello --output src/Commerce/Plugins
```

This scaffolds `Commerce.Plugin.Sample.Hello` with:

- `Plugin.json` manifest
- `ICommercePlugin` implementation
- SDK MSBuild import
- Localization folder

Set `CommercePluginSystemName` in the `.csproj` to match `systemName` in `Plugin.json` (e.g. `Sample.Hello`).

### 2. Implement your plugin

Implement `ICommercePlugin`:

```csharp
public sealed class HelloPlugin : ICommercePlugin
{
    public void RegisterServices(IServiceCollection services, IConfiguration configuration) { }

    public Task InitializeAsync(ICommercePluginContext context, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task StartAsync(ICommercePluginContext context, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task StopAsync(ICommercePluginContext context, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
```

### 3. Build

```bash
commerce plugin build --project Commerce.Plugin.Sample.Hello/Commerce.Plugin.Sample.Hello.csproj
```

Or `dotnet build`. The SDK target copies output to `Host/Commerce.Host/Plugins/{SystemName}/` when building inside the monorepo.

### 4. Validate

```bash
commerce plugin validate --project Commerce.Plugin.Sample.Hello/Commerce.Plugin.Sample.Hello.csproj
```

Validation checks:

- Forbidden project references (host, engine, module infrastructure)
- `Plugin.json` presence and schema
- Version compatibility (`minimumCommerceVersion`, optional `maximumCommerceVersion`)
- Assembly file in build output
- Dependency warnings when `--directory` targets an installed plugin set

### 5. Pack

```bash
commerce plugin pack --project Commerce.Plugin.Sample.Hello/Commerce.Plugin.Sample.Hello.csproj
```

Produces `{SystemName}.zip` containing manifest, assembly, localization, and optional `dependencies/` / `assets/` folders. **No plugin code runs during pack.**

### 6. Test

Use `Commerce.Plugin.Testing`:

```csharp
var host = new PluginTestHostBuilder()
    .WithManifestFile("Plugin.json")
    .WithPluginDirectory(outputDirectory)
    .ConfigureServices(services => services.AddSingleton<MyService>())
    .Build();

await host.RunLifecycleAsync(new HelloPlugin());
```

Run unit tests:

```bash
commerce plugin test --project path/to/test/project.csproj
```

## Plugin manifest (`Plugin.json`)

```json
{
  "systemName": "Sample.Hello",
  "name": "Hello Sample Plugin",
  "version": "1.0.0",
  "assembly": "Commerce.Plugin.Sample.Hello.dll",
  "minimumCommerceVersion": "1.0.0",
  "dependencies": []
}
```

### systemName rules

- Pattern: `Category.Name` (e.g. `Payment.Manual`, `Sample.Hello`)
- Must match `CommercePluginSystemName` in the project file

## Reference rules

Plugins **may** reference:

- `Commerce.Framework.PluginContracts`
- Module **Contracts** projects (e.g. `Commerce.Payments.Contracts`)
- `Commerce.Plugin.Contracts`, `Commerce.Plugin.Sdk` (dev tooling)

Plugins **must not** reference:

- `Commerce.Host`
- `Commerce.Framework.Plugins`
- Module Infrastructure / Application layers

See `PluginDevelopmentRules` in `Commerce.Plugin.Contracts`.

## Package layout

```
Sample.Hello/
  Plugin.json
  Commerce.Plugin.Sample.Hello.dll
  Localization/
    en.json
  dependencies/     (optional)
  assets/             (optional)
```

## CLI reference

| Command | Description |
|---------|-------------|
| `commerce plugin create` | Scaffold from template |
| `commerce plugin build` | `dotnet build` wrapper |
| `commerce plugin test` | `dotnet test` wrapper |
| `commerce plugin validate` | Manifest + project + output checks |
| `commerce plugin pack` | Create distributable ZIP |

Environment variable `COMMERCE_PLUGIN_TEMPLATE_PATH` overrides the template location.

## Example plugins in this repo

| Plugin | Purpose |
|--------|---------|
| `Commerce.Plugin.Test` | Full-featured test plugin (settings, permissions, migrations, UI metadata) |
| `Commerce.Plugin.Payment.Manual` | Manual payment provider |
| `Commerce.Plugin.Payment.ZarinPal` | Redirect gateway |
| `Commerce.Plugin.Payment.Stripe` | Checkout Session + webhooks |

## Architecture reference

See [PLUGIN-ARCHITECTURE.md](./PLUGIN-ARCHITECTURE.md) for runtime discovery, lifecycle, and admin operations.

## Security

Package validation and packing inspect files only. The host loads plugin assemblies only after manifest validation and trusted deployment — the same model as Phase 19. Do not execute plugin entry points during CI pack/validate steps.
