namespace Commerce.SmartstoreImport.Domain.Enums;

public enum ImportRunStatus
{
    Pending = 0,
    Running = 1,
    Completed = 2,
    CompletedWithWarnings = 3,
    Failed = 4
}

public enum ImportIssueSeverity
{
    Warning = 0,
    Error = 1
}

public enum ReconciliationClassification
{
    Match = 0,
    Missing = 1,
    Duplicate = 2,
    Transformed = 3,
    Invalid = 4,
    NotApplicable = 5
}
