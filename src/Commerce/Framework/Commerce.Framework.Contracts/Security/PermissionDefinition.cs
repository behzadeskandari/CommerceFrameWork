namespace Commerce.Framework.Contracts.Security;

public sealed record PermissionDefinition(
    string Name,
    string Description,
    string ModuleSystemName);
