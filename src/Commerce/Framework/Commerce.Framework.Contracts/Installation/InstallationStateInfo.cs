namespace Commerce.Framework.Contracts.Installation;

public sealed record InstallationStateInfo(
    InstallationStatus Status,
    InstallationStep CurrentStep,
    bool IsLocked,
    string? ApplicationVersion,
    DateTime? InstalledAtUtc,
    string? LastError);
