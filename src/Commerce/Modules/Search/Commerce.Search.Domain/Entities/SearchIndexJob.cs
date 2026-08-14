using Commerce.Framework.Core.Entities;
using Commerce.Search.Domain.Enums;

namespace Commerce.Search.Domain.Entities;

public sealed class SearchIndexJob : AggregateRoot
{
    public SearchIndexJobType JobType { get; private set; }
    public SearchIndexJobStatus Status { get; private set; }
    public int? ProductId { get; private set; }
    public int? StoreId { get; private set; }
    public string? ErrorMessage { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? ProcessedAtUtc { get; private set; }

    public static SearchIndexJob Create(SearchIndexJobType jobType, int? productId = null, int? storeId = null) =>
        new()
        {
            JobType = jobType,
            Status = SearchIndexJobStatus.Pending,
            ProductId = productId,
            StoreId = storeId,
            CreatedAtUtc = DateTime.UtcNow
        };

    public void MarkProcessing()
    {
        Status = SearchIndexJobStatus.Processing;
    }

    public void MarkCompleted()
    {
        Status = SearchIndexJobStatus.Completed;
        ProcessedAtUtc = DateTime.UtcNow;
        ErrorMessage = null;
    }

    public void MarkFailed(string errorMessage)
    {
        Status = SearchIndexJobStatus.Failed;
        ProcessedAtUtc = DateTime.UtcNow;
        ErrorMessage = errorMessage.Length > 2000 ? errorMessage[..2000] : errorMessage;
    }
}
