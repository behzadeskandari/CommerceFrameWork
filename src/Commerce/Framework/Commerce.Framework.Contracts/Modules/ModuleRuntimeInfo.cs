namespace Commerce.Framework.Contracts.Modules;

public sealed record ModuleRuntimeInfo(
    ModuleDescriptor Descriptor,
    ModuleState State,
    TimeSpan? StartupDuration,
    string? FailureReason);
