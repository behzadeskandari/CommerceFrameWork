namespace Commerce.Framework.Contracts.Modules;

public enum ModuleState
{
    Discovered = 0,
    Validated = 1,
    Registered = 2,
    Initialized = 3,
    Started = 4,
    Failed = 5,
    Disabled = 6
}
