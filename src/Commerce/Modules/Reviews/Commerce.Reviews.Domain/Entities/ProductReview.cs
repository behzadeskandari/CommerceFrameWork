using Commerce.Framework.Core.Entities;
using Commerce.Reviews.Domain.Enums;

namespace Commerce.Reviews.Domain.Entities;

public sealed class ProductReview : AggregateRoot
{
    public const int TitleMaxLength = 200;
    public const int ContentMaxLength = 4000;

    public int ProductId { get; private set; }

    public int CustomerId { get; private set; }

    public int StoreId { get; private set; }

    public int Rating { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public string Content { get; private set; } = string.Empty;

    public ReviewModerationStatus ModerationStatus { get; private set; }

    public bool IsVerifiedPurchase { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    private ProductReview()
    {
    }

    public static ProductReview Create(
        int productId,
        int customerId,
        int storeId,
        int rating,
        string title,
        string content,
        bool isVerifiedPurchase,
        DateTime utcNow)
    {
        ValidateIds(productId, customerId, storeId);
        ValidateRating(rating);
        ValidateText(title, content);

        return new ProductReview
        {
            ProductId = productId,
            CustomerId = customerId,
            StoreId = storeId,
            Rating = rating,
            Title = title.Trim(),
            Content = content.Trim(),
            ModerationStatus = ReviewModerationStatus.Pending,
            IsVerifiedPurchase = isVerifiedPurchase,
            CreatedAtUtc = utcNow,
            UpdatedAtUtc = utcNow
        };
    }

    public bool IsOwnedBy(int customerId) => CustomerId == customerId;

    public bool IsPublic => ModerationStatus == ReviewModerationStatus.Approved;

    public bool CanBeEditedByCustomer() => ModerationStatus == ReviewModerationStatus.Pending;

    public void UpdateByCustomer(int rating, string title, string content, DateTime utcNow)
    {
        if (!CanBeEditedByCustomer())
        {
            throw new InvalidOperationException("Only pending reviews can be edited.");
        }

        ValidateRating(rating);
        ValidateText(title, content);
        Rating = rating;
        Title = title.Trim();
        Content = content.Trim();
        UpdatedAtUtc = utcNow;
    }

    public void Approve(DateTime utcNow)
    {
        ModerationStatus = ReviewModerationStatus.Approved;
        UpdatedAtUtc = utcNow;
    }

    public void Reject(DateTime utcNow)
    {
        ModerationStatus = ReviewModerationStatus.Rejected;
        UpdatedAtUtc = utcNow;
    }

    private static void ValidateIds(int productId, int customerId, int storeId)
    {
        if (productId <= 0 || customerId <= 0 || storeId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(productId));
        }
    }

    private static void ValidateRating(int rating)
    {
        if (!RatingScale.IsValid(rating))
        {
            throw new ArgumentOutOfRangeException(nameof(rating), "Rating must be between 1 and 5.");
        }
    }

    private static void ValidateText(string title, string content)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Review title is required.", nameof(title));
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            throw new ArgumentException("Review content is required.", nameof(content));
        }

        if (title.Length > TitleMaxLength)
        {
            throw new ArgumentException($"Title cannot exceed {TitleMaxLength} characters.", nameof(title));
        }

        if (content.Length > ContentMaxLength)
        {
            throw new ArgumentException($"Content cannot exceed {ContentMaxLength} characters.", nameof(content));
        }
    }
}
