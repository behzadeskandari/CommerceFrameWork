namespace Commerce.Framework.Contracts.Modules;

public sealed record ModuleDependency(string ModuleSystemName, string? MinimumVersion = null);
