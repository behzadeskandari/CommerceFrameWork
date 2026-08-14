using Commerce.Reviews.Domain;

namespace Commerce.Reviews.Application.Rating;

public static class ProductRatingCalculator
{
    public static (double AverageRating, IReadOnlyDictionary<int, int> Distribution) Compute(
        IEnumerable<int> approvedRatings)
    {
        var distribution = Enumerable.Range(RatingScale.MinRating, RatingScale.MaxRating)
            .ToDictionary(star => star, _ => 0);

        var ratings = approvedRatings.ToList();
        if (ratings.Count == 0)
        {
            return (0, distribution);
        }

        foreach (var rating in ratings)
        {
            if (distribution.ContainsKey(rating))
            {
                distribution[rating]++;
            }
        }

        var average = Math.Round(ratings.Average(), 1, MidpointRounding.AwayFromZero);
        return (average, distribution);
    }
}
