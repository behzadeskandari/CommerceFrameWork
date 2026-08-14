# Phase 41 — Plugin SDK / Developer Experience (Pre-Implementation)

## Goal

Deliver a first-class developer experience on top of the Phase 19 plugin runtime without introducing arbitrary code execution during package validation or packing.

## Scope

| Component | Purpose |
|-----------|---------|
| `Commerce.Plugin.Contracts` | Developer-facing contracts, package layout rules, compatibility models |
| `Commerce.Plugin.Sdk` | Validation, ZIP packing, archive validation, MSBuild targets |
| `Commerce.Plugin.Testing` | `PluginTestHostBuilder`, manifest factory |
| `Commerce.Plugin.Template` | Scaffold template consumed by CLI |
| `Commerce.Plugin.Cli` | `commerce plugin create/build/test/pack/validate` |
| `PluginPackageService` (restore) | Runtime ZIP validation/extraction for admin upload |

## Non-goals

- Running plugin assemblies during pack/validate
- Publishing NuGet packages to a public feed (local/tool workflow only)
- Changing plugin runtime lifecycle semantics

## CLI commands

```
commerce plugin create --category Sample --name Hello [--output ./plugins]
commerce plugin build [--project ./Commerce.Plugin.Sample.Hello/Commerce.Plugin.Sample.Hello.csproj]
commerce plugin test [--project ./tests/...]
commerce plugin validate [--project ...] [--directory ...] [--commerce-version 1.0.0]
commerce plugin pack [--project ...] [--directory ...] [--output ./Sample.Hello.zip]
```

## Security model

Pack and validate operate on static files only:

- ZIP entry path traversal checks
- Size and file-count limits
- Manifest field validation
- Assembly presence checks (file/entry existence)

Plugin code executes only after trusted host load — unchanged from Phase 19.

## Tests

- `Commerce.Tests.Plugin.Sdk` — validator, packer, archive security, test host

## Documentation

- `docs/commerce/PLUGIN-DEVELOPMENT.md` — author guide
- `docs/commerce/PHASE-41-REPORT.md` — completion report
