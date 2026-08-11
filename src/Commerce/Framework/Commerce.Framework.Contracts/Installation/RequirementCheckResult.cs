namespace Commerce.Framework.Contracts.Installation;

public sealed record RequirementCheckResult(
    string Name,
    bool IsSatisfied,
    string Message);
