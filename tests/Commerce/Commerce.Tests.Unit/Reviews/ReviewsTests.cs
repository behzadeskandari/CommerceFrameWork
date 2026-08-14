using Commerce.Reviews.Application.Rating;
using Commerce.Reviews.Domain;
using Commerce.Reviews.Domain.Entities;
using Commerce.Reviews.Domain.Enums;

namespace Commerce.Tests.Unit.Reviews;

public sealed class ProductReviewDomainTests
{
    [Fact]
    public void Create_StartsPending_WithVerifiedFlag()
    {
        var review = ProductReview.Create(1, 10, 1, 5, "Great", "Loved it", true, DateTime.UtcNow);

        Assert.Equal(ReviewModerationStatus.Pending, review.ModerationStatus);
        Assert.True(review.IsVerifiedPurchase);
        Assert.False(review.IsPublic);
    }

    [Fact]
    public void Approve_MakesReviewPublic()
    {
        var review = ProductReview.Create(1, 10, 1, 4, "Good", "Nice product", false, DateTime.UtcNow);
        review.Approve(DateTime.UtcNow);

        Assert.Equal(ReviewModerationStatus.Approved, review.ModerationStatus);
        Assert.True(review.IsPublic);
    }

    [Fact]
    public void Reject_HidesReviewFromPublic()
    {
        var review = ProductReview.Create(1, 10, 1, 2, "Bad", "Not good", false, DateTime.UtcNow);
        review.Reject(DateTime.UtcNow);

        Assert.Equal(ReviewModerationStatus.Rejected, review.ModerationStatus);
        Assert.False(review.IsPublic);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void Create_RejectsInvalidRating(int rating)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ProductReview.Create(1, 10, 1, rating, "Title", "Content", false, DateTime.UtcNow));
    }

    [Fact]
    public void UpdateByCustomer_AllowsPendingReviewOnly()
    {
        var review = ProductReview.Create(1, 10, 1, 3, "Ok", "Average", false, DateTime.UtcNow);
        review.UpdateByCustomer(4, "Better", "Updated thoughts", DateTime.UtcNow);

        Assert.Equal(4, review.Rating);
        Assert.Equal("Better", review.Title);
    }

    [Fact]
    public void UpdateByCustomer_BlocksApprovedReview()
    {
        var review = ProductReview.Create(1, 10, 1, 5, "Great", "Nice", false, DateTime.UtcNow);
        review.Approve(DateTime.UtcNow);

        Assert.Throws<InvalidOperationException>(() =>
            review.UpdateByCustomer(1, "Changed", "Too late", DateTime.UtcNow));
    }

    [Fact]
    public void IsOwnedBy_ValidatesCustomer()
    {
        var review = ProductReview.Create(1, 10, 1, 5, "Great", "Nice", false, DateTime.UtcNow);

        Assert.True(review.IsOwnedBy(10));
        Assert.False(review.IsOwnedBy(11));
    }
}

public sealed class WishlistDomainTests
{
    [Fact]
    public void AddProduct_TracksItems()
    {
        var wishlist = Wishlist.Create(10, 1);
        var utcNow = DateTime.UtcNow;

        wishlist.AddProduct(100, utcNow);

        Assert.True(wishlist.ContainsProduct(100));
        Assert.Single(wishlist.Items);
    }

    [Fact]
    public void AddProduct_PreventsDuplicates()
    {
        var wishlist = Wishlist.Create(10, 1);
        wishlist.AddProduct(100, DateTime.UtcNow);

        Assert.Throws<InvalidOperationException>(() => wishlist.AddProduct(100, DateTime.UtcNow));
    }

    [Fact]
    public void RemoveProduct_RemovesExistingItem()
    {
        var wishlist = Wishlist.Create(10, 1);
        wishlist.AddProduct(100, DateTime.UtcNow);

        Assert.True(wishlist.RemoveProduct(100));
        Assert.False(wishlist.ContainsProduct(100));
    }

    [Fact]
    public void IsOwnedBy_ValidatesCustomer()
    {
        var wishlist = Wishlist.Create(10, 1);

        Assert.True(wishlist.IsOwnedBy(10));
        Assert.False(wishlist.IsOwnedBy(11));
    }
}

public sealed class ProductRatingCalculatorTests
{
    [Fact]
    public void Compute_UsesApprovedRatingsOnlyPattern()
    {
        var (average, distribution) = ProductRatingCalculator.Compute([5, 4, 4]);

        Assert.Equal(4.3, average);
        Assert.Equal(0, distribution[1]);
        Assert.Equal(0, distribution[2]);
        Assert.Equal(0, distribution[3]);
        Assert.Equal(2, distribution[4]);
        Assert.Equal(1, distribution[5]);
    }

    [Fact]
    public void Compute_ReturnsZeroAverageWhenEmpty()
    {
        var (average, distribution) = ProductRatingCalculator.Compute([]);

        Assert.Equal(0, average);
        Assert.Equal(RatingScale.MaxRating, distribution.Count);
    }
}

public sealed class RatingScaleTests
{
    [Theory]
    [InlineData(1, true)]
    [InlineData(5, true)]
    [InlineData(0, false)]
    [InlineData(6, false)]
    public void IsValid_EnforcesOneToFive(int rating, bool expected) =>
        Assert.Equal(expected, RatingScale.IsValid(rating));
}
