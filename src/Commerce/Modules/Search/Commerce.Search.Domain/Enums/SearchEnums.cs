namespace Commerce.Search.Domain.Enums;

public enum SearchIndexJobType
{
    FullRebuild = 1,
    ProductUpsert = 2,
    ProductDelete = 3
}

public enum SearchIndexJobStatus
{
    Pending = 1,
    Processing = 2,
    Completed = 3,
    Failed = 4
}
