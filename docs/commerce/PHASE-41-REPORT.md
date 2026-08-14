# Phase 41 — Plugin SDK / Developer Experience (Report)

**Status:** Complete  
**Builds on:** Phase 18–19 plugin runtime

## Summary

Phase 41 adds a plugin SDK, developer CLI, project template, testing utilities, restored package service, and author documentation. Validate/pack remain static-file operations with no plugin code execution.

## Deliverables

### Projects (`src/Commerce/PluginSdk/`)

| Project | Description |
|---------|-------------|
| `Commerce.Plugin.Contracts` | Package layout constants, reference rules, compatibility info |
| `Commerce.Plugin.Sdk` | `PluginSdkValidator`, `PluginPackagePacker`, `PluginArchiveValidator`, MSBuild targets |
| `Commerce.Plugin.Testing` | `PluginTestHostBuilder`, `PluginManifestTestFactory` |
| `Commerce.Plugin.Template` | Tokenized scaffold (`content/__PROJECT_NAME__/`) |
| `Commerce.Plugin.Cli` | Global tool `commerce` with `plugin` subcommands |

### Runtime fix

- Restored `IPluginPackageService` + `PluginPackageService` under `Commerce.Framework.Plugins/Packages/`
- Moved manifest parser/validator to `Commerce.Framework.PluginContracts.Manifest` (shared by runtime and SDK)
- Added `PluginPackagePathSecurity.IsPathTraversal` in contracts

### MSBuild

- `Commerce.Plugin.Sdk.targets` — copies build output, `Plugin.json`, localization, dependencies, assets to host `Plugins/{SystemName}/`
- `Commerce.Plugin.Test` migrated to SDK targets

### CLI

Install locally:

```bash
dotnet tool install --global --add-source ./src/Commerce/PluginSdk/Commerce.Plugin.Cli/bin/Debug Commerce.Plugin.Cli
```

Or run from repo:

```bash
dotnet run --project src/Commerce/PluginSdk/Commerce.Plugin.Cli -- plugin create --category Sample --name Hello --output ./plugins
```

### Example plugins

- **`Commerce.Plugin.Test`** — full runtime reference (settings, permissions, migrations, localization)
- **Template scaffold** — minimal `ICommercePlugin` starter via `commerce plugin create`

### Tests

`Commerce.Tests.Plugin.Sdk` — **9 passing**

- Manifest/directory validation
- Compatibility evaluation
- ZIP pack + archive validation
- Path traversal detection
- Plugin test host builder

### Documentation

- [`PLUGIN-DEVELOPMENT.md`](./PLUGIN-DEVELOPMENT.md) — developer guide

## Verification

```bash
dotnet build src/Commerce/PluginSdk/Commerce.Plugin.Sdk/Commerce.Plugin.Sdk.csproj
dotnet build src/Commerce/PluginSdk/Commerce.Plugin.Cli/Commerce.Plugin.Cli.csproj
dotnet build src/Commerce/Plugins/Test/Commerce.Plugin.Test/Commerce.Plugin.Test.csproj
dotnet test tests/Commerce/Commerce.Tests.Plugin.Sdk/Commerce.Tests.Plugin.Sdk.csproj
```

**Note:** `Commerce.Framework.Plugins` has pre-existing compile errors unrelated to Phase 41 (ambiguous DTO types, sealed `CommerceDbContext` in dev seeder). SDK/CLI/tests build independently.

## Security

| Operation | Executes plugin code? |
|-----------|----------------------|
| `commerce plugin validate` | No |
| `commerce plugin pack` | No |
| Admin ZIP upload (`ValidatePackageAsync`) | No |
| Host plugin load (Phase 19) | Yes — trusted after validation |

## Next steps (out of scope)

- Publish SDK packages to NuGet
- Migrate all payment/shipping plugins to SDK MSBuild targets
- Fix pre-existing `Commerce.Framework.Plugins` build errors
